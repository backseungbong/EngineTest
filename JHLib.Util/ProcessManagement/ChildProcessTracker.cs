using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace JHLib.Util.Helper
{
    /// <summary>
    /// 이 부모 프로세스가 예기치 않게 종료(비정상 종료 포함)될 경우,
    /// 자식 프로세스들이 자동으로 강제 종료되도록 합니다.
    /// Windows 10 이상에서 동작합니다.
    /// </summary>
    /// <remarks>
    /// Windows Job Object의 JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE 플래그를 사용합니다.
    /// 프로세스(부모)가 종료되면 Windows가 자동으로 열려 있는 모든 Job 핸들을 닫고,
    /// 이때 연결된 자식 프로세스들도 즉시 강제 종료됩니다.
    /// 
    /// 두 가지 사용 모드를 지원합니다:
    /// 1. TrackCurrentProcess(): 현재 프로세스를 Job에 등록하면, 
    ///    이후 생성되는 모든 자식 프로세스가 자동으로 Job에 포함됩니다.
    /// 2. AddProcess(process): 특정 자식 프로세스만 개별적으로 Job에 등록합니다.
    /// 
    /// 참고 자료:
    /// https://learn.microsoft.com/en-us/windows/win32/procthread/job-objects
    /// https://devblogs.microsoft.com/oldnewthing/20131209-00/?p=2433
    /// https://www.meziantou.net/killing-all-child-processes-when-the-parent-exits-job-object.htm
    /// </remarks>
    public static unsafe partial class ChildProcessTracker
    {
        [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16)]
        private static partial nint CreateJobObjectW(SecurityAttributes* lpJobAttributes, string name);

        [LibraryImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool SetInformationJobObject(nint job, JobObjectInfoType infoType,
            JOBOBJECT_EXTENDED_LIMIT_INFORMATION* lpJobObjectInfo, uint cbJobObjectInfoLength);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool AssignProcessToJobObject(nint job, nint process);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool CloseHandle(nint hObject);

        // Job 핸들은 프로세스 수명 전체에 걸쳐 유지됩니다.
        // 프로세스 종료 시 Windows가 자동으로 핸들을 닫고, 연결된 자식들도 종료됩니다.
        private static readonly nint _jobHandle;

        /// <summary>
        /// 현재 프로세스(부모)를 Job에 등록합니다.
        /// 이후 이 프로세스가 생성하는 모든 자식 프로세스는 자동으로 Job에 포함되므로,
        /// 부모 종료 시 자식들도 자동 종료됩니다.
        /// AddProcess()를 개별 호출할 필요가 없어집니다.
        /// </summary>
        /// <exception cref="InvalidOperationException">Job 핸들이 초기화되지 않은 경우</exception>
        /// <exception cref="Win32Exception">Job 등록 실패 시</exception>
        public static void TrackCurrentProcess()
        {
            if (_jobHandle == 0)
                throw new InvalidOperationException("ChildProcessTracker is not available on this operating system.");

            if (!AssignProcessToJobObject(_jobHandle, Process.GetCurrentProcess().Handle))
                throw new Win32Exception();
        }

        /// <summary>
        /// 추적할 자식 프로세스를 개별 등록합니다.
        /// 현재 프로세스(부모)가 종료되면, 추적 중인 자식 프로세스도 자동으로 함께 종료됩니다.
        /// 만약 자식 프로세스가 먼저 종료되더라도 문제없습니다.
        /// </summary>
        /// <param name="process">추적할 자식 프로세스 객체</param>
        /// <exception cref="InvalidOperationException">Job 핸들이 초기화되지 않은 경우</exception>
        /// <exception cref="Win32Exception">Job 등록 실패 시 (권한 문제 등)</exception>
        public static void AddProcess(Process process)
        {
            if (_jobHandle == 0)
                throw new InvalidOperationException("ChildProcessTracker is not available on this operating system.");

            if (process.HasExited) // 이미 종료된 프로세스는 등록 불가
                return;

            var success = AssignProcessToJobObject(_jobHandle, process.Handle);
            if (success == false && process.HasExited == false)
                throw new Win32Exception();
        }

        static ChildProcessTracker()
        {
            if (!OperatingSystem.IsWindows())
                return;

            // bInheritHandle = false를 명시하여, 자식 프로세스가 Job 핸들을 상속받지 않도록 합니다.
            // 자식이 핸들을 상속받으면 부모가 죽어도 핸들 참조가 남아
            // KILL_ON_JOB_CLOSE가 동작하지 않을 수 있습니다.
            var securityAttributes = new SecurityAttributes
            {
                nLength = (uint)sizeof(SecurityAttributes),
                lpSecurityDescriptor = 0,
                bInheritHandle = 0 // FALSE: 핸들 상속 차단
            };

            // Job 이름은 선택 사항(null 가능)이지만 진단(Diagnostics)에 도움이 됩니다.
            // SysInternals Handle 유틸리티로 확인 가능: handle -a ChildProcessTracker
            var jobName = "ChildProcessTracker" + Environment.ProcessId;

            _jobHandle = CreateJobObjectW(&securityAttributes, jobName);

            if (_jobHandle == 0)
                throw new InvalidOperationException("Could not create Job object for ChildProcessTracker.");

            // 핵심 설정: 우리 프로세스가 종료되어 Job 핸들이 닫히면,
            // Windows가 해당 Job에 속한 자식 프로세스들도 자동으로 종료합니다.
            var extendedInfo = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION
            {
                BasicLimitInformation = new JOBOBJECT_BASIC_LIMIT_INFORMATION
                {
                    LimitFlags = JOBOBJECTLIMIT.JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
                }
            };

            if (!SetInformationJobObject(
                _jobHandle,
                JobObjectInfoType.ExtendedLimitInformation,
                &extendedInfo,
                (uint)sizeof(JOBOBJECT_EXTENDED_LIMIT_INFORMATION)))
            {
                throw new Win32Exception();
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SecurityAttributes
    {
        public uint nLength;
        public nint lpSecurityDescriptor;
        public int bInheritHandle; // BOOL: 0 = FALSE, 1 = TRUE
    }

    public enum JobObjectInfoType
    {
        AssociateCompletionPortInformation = 7,
        BasicLimitInformation = 2,
        BasicUIRestrictions = 4,
        EndOfJobTimeInformation = 6,
        ExtendedLimitInformation = 9,
        SecurityLimitInformation = 5,
        GroupInformation = 11
    }

    [Flags]
    public enum JOBOBJECTLIMIT : uint
    {
        /// <summary>Job 핸들이 닫힐 때 프로세스 강제 종료</summary>
        JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x2000
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct IO_COUNTERS
    {
        public ulong ReadOperationCount;
        public ulong WriteOperationCount;
        public ulong OtherOperationCount;
        public ulong ReadTransferCount;
        public ulong WriteTransferCount;
        public ulong OtherTransferCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct JOBOBJECT_BASIC_LIMIT_INFORMATION
    {
        public long PerProcessUserTimeLimit;
        public long PerJobUserTimeLimit;
        public JOBOBJECTLIMIT LimitFlags;
        public nuint MinimumWorkingSetSize;
        public nuint MaximumWorkingSetSize;
        public uint ActiveProcessLimit;
        public long Affinity;
        public uint PriorityClass;
        public uint SchedulingClass;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
    {
        public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
        public IO_COUNTERS IoInfo;
        public nuint ProcessMemoryLimit;
        public nuint JobMemoryLimit;
        public nuint PeakProcessMemoryUsed;
        public nuint PeakJobMemoryUsed;
    }
}