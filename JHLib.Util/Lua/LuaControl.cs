using JHLib.Util.ArrayControl;
using JHLib.Util.DataStream;
using JHLib.Util.Struct;
using System.Runtime.InteropServices;
using System.Text;

namespace JHLib.Util.Lua
{
    public enum LuaStatus
    {
        /// <summary> success </summary>
        OK = 0,
        /// <summary> yield </summary>
        Yield = 1,
        /// <summary> runtime error </summary>
        ErrRun = 2,
        /// <summary> syntax error during precompilation </summary>
        ErrSyntax = 3,
        /// <summary> memory allocation error. For such errors, Lua does not call the message handler </summary>
        ErrMem = 4,
        /// <summary> error while running the message handler </summary>
        ErrErr = 5,
    }

    public enum LuaType
    {
        None = -1,
        /// <summary> LUA_TNIL </summary>
        Nil = 0,
        /// <summary> LUA_TBOOLEAN</summary>
        Boolean = 1,
        /// <summary> LUA_TLIGHTUSERDATA </summary>
        LightUserData = 2,
        /// <summary> LUA_TNUMBER</summary>
        Number = 3,
        /// <summary> LUA_TSTRING </summary>
        String = 4,
        /// <summary> LUA_TTABLE </summary>
        Table = 5,
        /// <summary> LUA_TFUNCTION</summary>
        Function = 6,
        /// <summary> LUA_TUSERDATA </summary>
        UserData = 7,
        /// <summary> LUA_TTHREAD </summary>
        Thread = 8,
    }

    public unsafe static class LuaControl
    {
        private const int LUA_REGISTRYINDEX = -10000;
        private const int LUA_ENVIRONINDEX = -10001;
        private const int LUA_GLOBALSINDEX = -10002;

        public static IntPtr NewState() => LuaNative.luaL_newstate();
        public static void OpenLibs(IntPtr hLua) => LuaNative.luaL_openlibs(hLua);
        public static void Close(IntPtr hLua)
        {
            if (hLua != 0)
                LuaNative.lua_close(hLua);
        }

        public static LuaStatus LoadFile(IntPtr hLua, string fileName) => (LuaStatus)LuaNative.luaL_loadfile(hLua, fileName);
        public static void CreateTable(IntPtr hLua, int tableLength) => LuaNative.lua_createtable(hLua, tableLength, 0);
        public static void Rawseti(IntPtr hLua, int stackIndex, int index) => LuaNative.lua_rawseti(hLua, stackIndex, index);
        public static LuaStatus PCall(IntPtr hLua, int nArg = 0, int nRst = 0) => LuaNative.lua_pcall(hLua, nArg, nRst, 0);
        public static void Call(IntPtr hLua, int nArg = 0, int nRst = 0) => LuaNative.lua_pcall(hLua, nArg, nRst, 0);

        public static int CreateFunctionRefID(IntPtr hLua, string functionName)
        {
            LuaNative.lua_getfield(hLua, LUA_GLOBALSINDEX, functionName);
            return LuaNative.luaL_ref(hLua, LUA_REGISTRYINDEX);
        }

        public static void Unref(IntPtr hLua, int refID) => LuaNative.luaL_unref(hLua, LUA_REGISTRYINDEX, refID);
        public static void GetFunction(IntPtr hLua, string functionName) => LuaNative.lua_getfield(hLua, LUA_GLOBALSINDEX, functionName);
        public static void GetFunction(IntPtr hLua, int refid) => LuaNative.lua_rawgeti(hLua, LUA_REGISTRYINDEX, refid);

        public static LuaType TopType(IntPtr hLua) => (LuaType)LuaNative.lua_type(hLua, -1);
        public static int Top(IntPtr hLua) => LuaNative.lua_gettop(hLua);
        public static void PushNil(IntPtr hLua) => LuaNative.lua_pushnil(hLua);
        public static void Push(IntPtr hLua, bool v) => LuaNative.lua_pushboolean(hLua, v ? 1 : 0);
        public static void Push(IntPtr hLua, int v) => LuaNative.lua_pushinteger(hLua, v);
        public static void Push(IntPtr hLua, long v) => LuaNative.lua_pushinteger(hLua, v);
        public static void Push(IntPtr hLua, double v) => LuaNative.lua_pushnumber(hLua, v);


        public static void Push(IntPtr hLua, bool? v)
        {
            if (v.HasValue)
                Push(hLua, v.Value);
            else
                PushNil(hLua);
        }
        public static void Push(IntPtr hLua, int? v)
        {
            if (v.HasValue)
                Push(hLua, v.Value);
            else
                PushNil(hLua);
        }
        public static void Push(IntPtr hLua, long? v)
        {
            if (v.HasValue)
                Push(hLua, v.Value);
            else
                PushNil(hLua);
        }
        public static void Push(IntPtr hLua, double? v)
        {
            if (v.HasValue)
                Push(hLua, v.Value);
            else
                PushNil(hLua);
        }

        public static void Push(IntPtr hLua, byte* p, int l) =>
            LuaNative.lua_pushlstring(hLua, p, l);
        public static void Push(IntPtr hLua, ref byte p, int l) =>
            LuaNative.lua_pushlstring(hLua, ref p, l);
        public static void Push(IntPtr hLua, DataRange range) =>
            LuaNative.lua_pushlstring(hLua, ref range.Data0, range.Count);
        public static void Push(IntPtr hLua, DataHeaderReader reader) =>
            LuaNative.lua_pushlstring(hLua, ref reader.Data0<byte>(), reader.Count);
        public static void PushUTF8(IntPtr hLua, string s) =>
            Push(hLua, Encoding.UTF8.GetBytes(s));
        public static void PushEmptyString(IntPtr hLua)
        {
            var tempPtr = 0;
            LuaNative.lua_pushlstring(hLua, (nint)(&tempPtr), 0);
        }

        public static void Push(IntPtr hLua, byte[] arr, bool null2empty = false)
        {
            if (arr != null)
            {
                fixed (byte* p = &MemoryMarshal.GetArrayDataReference(arr))
                {
                    LuaNative.lua_pushlstring(hLua, (nint)p, (nuint)arr.Length);
                    return;
                }
            }
            else if (null2empty) { PushEmptyString(hLua); }
            else LuaNative.lua_pushnil(hLua);
        }


        public static void Push(IntPtr hLua, int[] arr, bool null2empty = false)
        {
            if (arr != null) PushInternal(hLua, arr);
            else if (null2empty) LuaNative.lua_createtable(hLua, 0, 0);
            else LuaNative.lua_pushnil(hLua);
        }
        private static void PushInternal(IntPtr hLua, int[] ds)
        {
            var c = ds.Length; LuaNative.lua_createtable(hLua, c, 0);
            if (c != 0)
            {
                var i = 0;
                do
                {
                    LuaNative.lua_pushinteger(hLua, ds[i]);
                    LuaNative.lua_rawseti(hLua, -2, ++i);
                }
                while (i < c);
            }
        }


        public static void Push(IntPtr hLua, long[] arr, bool null2empty = false)
        {
            if (arr != null) PushInternal(hLua, arr);
            else if (null2empty) LuaNative.lua_createtable(hLua, 0, 0);
            else LuaNative.lua_pushnil(hLua);
        }
        private static void PushInternal(IntPtr hLua, long[] ds)
        {
            var c = ds.Length; LuaNative.lua_createtable(hLua, c, 0);
            if (c != 0)
            {
                var i = 0;
                do
                {
                    LuaNative.lua_pushinteger(hLua, ds[i]);
                    LuaNative.lua_rawseti(hLua, -2, ++i);
                }
                while (i < c);
            }
        }


        public static void Push(IntPtr hLua, double[] arr, bool null2empty = false)
        {
            if (arr != null) PushInternal(hLua, arr);
            else if (null2empty) LuaNative.lua_createtable(hLua, 0, 0);
            else LuaNative.lua_pushnil(hLua);
        }
        private static void PushInternal(IntPtr hLua, double[] ds)
        {
            var c = ds.Length; LuaNative.lua_createtable(hLua, c, 0);
            if (c != 0)
            {
                var i = 0;
                do
                {
                    LuaNative.lua_pushnumber(hLua, ds[i]);
                    LuaNative.lua_rawseti(hLua, -2, ++i);
                }
                while (i < c);
            }
        }


        public static void Push(IntPtr hLua, byte[][] arr, bool null2empty = false)
        {
            if (arr != null) PushInternal(hLua, arr);
            else if (null2empty) LuaNative.lua_createtable(hLua, 0, 0);
            else LuaNative.lua_pushnil(hLua);
        }
        private static void PushInternal(IntPtr hLua, byte[][] ds)
        {
            var c = ds.Length; LuaNative.lua_createtable(hLua, c, 0);
            if (c != 0)
            {
                var i = 0;
                do
                {
                    Push(hLua, ds[i]);
                    LuaNative.lua_rawseti(hLua, -2, ++i);
                }
                while (i < c);
            }
        }


        public static void PeekStream(IntPtr hLua, int index, out byte* p0, out int len) =>
            LuaNative.lua_tolstring(hLua, index, out p0, out len);

        public static byte[] PopBytes(IntPtr hLua)
        {
            LuaNative.lua_tolstring(hLua, -1, out var p0, out var len);
            var result = AC.CopyNew(ref *p0, len);
            LuaNative.lua_settop(hLua, -2);
            return result;
        }

        public static string PopASCII(IntPtr hLua)
        {
            LuaNative.lua_tolstring(hLua, -1, out var p0, out var len);
            var result = Encoding.ASCII.GetString(p0, len);
            LuaNative.lua_settop(hLua, -2);
            return result;
        }

        public static string PopUTF8(IntPtr hLua)
        {
            LuaNative.lua_tolstring(hLua, -1, out var p0, out var len);
            var result = Encoding.UTF8.GetString(p0, len);
            LuaNative.lua_settop(hLua, -2);
            return result;
        }

        public static void Pop(IntPtr hLua, int count) => LuaNative.lua_settop(hLua, -1 - count);
        public static void Pop(IntPtr hLua) => LuaNative.lua_settop(hLua, -2);

        public static void SetTableField(IntPtr hLua, string tableKey, string indexKey, string value)
        {
            LuaNative.lua_getfield(hLua, LUA_GLOBALSINDEX, tableKey);
            LuaNative.lua_pushansi(hLua, value);  // 파일 관련한 작업은 ANSI 형태로 처리해야 내부적으로 문제가 없는듯
            LuaNative.lua_setfield(hLua, -2, indexKey);
            LuaNative.lua_settop(hLua, -2);
        }

        public static void RegFunction(IntPtr hLua, string functionName, delegate* unmanaged[Cdecl]<nint, int> functionPtr)
        {
            LuaNative.lua_pushcclosure(hLua, (nint)functionPtr, 0);
            LuaNative.lua_setfield(hLua, LUA_GLOBALSINDEX, functionName);
        }
    }
}