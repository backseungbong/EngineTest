using JHLib.Util.FileIO;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JHLib.Util.Config
{
    /// <summary> 
    /// JSON 형식을 사용하여 설정 파일을 관리(로드/저장)한다 <br/>
    /// 저장 및 로드시 원자적 처리와 검증파일을 사용하여 파일 손상방지 및 복구를 지원한다
    /// </summary>
    public static class JsonConfig
    {
        /// <summary> JSON 직렬화 및 역직렬화에 사용되는 기본 옵션 설정 </summary>
        private static readonly JsonSerializerOptions Options = new()
        {
            IncludeFields = true, // 필드 포함
            WriteIndented = true, // 들여쓰기 저장
            AllowTrailingCommas = true, // 마지막 콤마 허용 (수정 편의성)
            PropertyNameCaseInsensitive = true, // 대소문자 구분 없이 읽기
            ReadCommentHandling = JsonCommentHandling.Skip, // 주석 허용 (주석내용을 파싱하지 않음)
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip, // 매핑할 속성이 없어도 에러를 내지 않음
            DefaultIgnoreCondition = JsonIgnoreCondition.Never, // null 값 저장
            NumberHandling = JsonNumberHandling.AllowReadingFromString, // 숫자를 문자열로 저장해도 읽기 허용
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // 한글 깨짐 방지 
        };

        /// <summary>
        /// 지정된 경로에서 JSON 설정 파일을 읽어온다 <br/>
        /// - 파일이 없거나 내용이 비어있는 경우 : new 객체를 생성해 파일로 저장 및 반환 <br/>
        /// - 내용이 있는 파일에서 Json 파싱 실패시 : JsonException 에러
        /// </summary>
        /// <param name="path">불러올 파일 경로</param>
        /// <exception cref="JsonException">파일 내용이 올바른 JSON 형식이 아닐 때 발생</exception>
        public static T Load<T>(string path) where T : class, new()
        {
            if (File.Exists(path) == false)
                return SaveDefault<T>(path);

            var content = ValidationFile.Read(path);
            if (content == null || content.Length == 0)
                return SaveDefault<T>(path);

            return JsonSerializer.Deserialize<T>(content, Options);
        }

        /// <summary>
        /// 지정된 경로에서 JSON 설정 파일을 읽어온다<br/>
        /// - 파일이 없거나 내용이 비어있는 경우 : new 객체를 생성해 파일로 저장 및 반환 <br/>
        /// - 내용이 있는 파일에서 Json 파싱 실패시 : 예외 전파 없이 new 객체 반환
        /// </summary>
        /// <param name="path">불러올 파일 경로</param>
        public static T LoadOrDefault<T>(string path) where T : class, new()
        {
            try
            {
                return Load<T>(path);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[JsonConfig] Load Failed: {ex.Message}");
                return new T();
            }
        }

        /// <summary> 
        /// 설정 객체를 JSON 형식으로 지정된 경로에 저장한다 
        /// </summary>
        /// <typeparam name="T">설정 데이터 클래스 타입</typeparam>
        /// <param name="path">저장할 파일 경로</param>
        /// <param name="data">저장할 설정 객체</param>
        public static void Save<T>(string path, T data) where T : class, new()
        {
            try
            {
                var content = JsonSerializer.SerializeToUtf8Bytes(data, Options);
                ValidationFile.Write(path, content);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[JsonConfig] Save Failed: {ex.Message}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static T SaveDefault<T>(string path) where T : class, new()
        {
            var result = new T();
            Save(path, result);
            return result;
        }
    }
}