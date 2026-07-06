using JHLib.Util.Time;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Documents;

namespace JHLib.S57ManualUpdate.ManualUpdate
{
    public class ManualUpdateManager
    {
        public ManualUpdateManager(string exePath)
        {
            _updateFilePath = Path.Combine(exePath, "S57", "ENC", "UPDATE");
            if (Directory.Exists(_updateFilePath) == false) Directory.CreateDirectory(_updateFilePath);

            // 전체 데이터 읽어오기 
            LoadAllManualUpdate();
        }

        private string _updateFilePath = "";

        private Dictionary<string, List<MUmain>> _dicManualUpdate = new();
        private readonly object _lockMU = new object();

        // 옵션 설정 (Enum을 문자열로 저장하거나 들여쓰기 설정 등)
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = false, // true로 하면 사람이 읽기 좋으나 용량이 커짐
            IncludeFields = true,
            Converters = { new JsonStringEnumConverter() }
        };

        public void LoadAllManualUpdate()
        {
            _dicManualUpdate.Clear();

            string[] files = Directory.GetFiles(_updateFilePath, "*.mud");
            foreach (string filePath in files)
            {
                var chartName = Path.GetFileNameWithoutExtension(filePath);
                try
                {
                    using var stream = File.OpenRead(filePath);
                    var loadedData = JsonSerializer.Deserialize<List<MUmain>>(stream, _jsonOptions);
                    if (loadedData != null)
                    {
                        lock (_lockMU) _dicManualUpdate[chartName] = loadedData;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"로드 실패: {ex.Message}");
                }
            }
        }

        public void ClearAllManualUpdate()
        {
            string[] files = Directory.GetFiles(_updateFilePath, "*.mud");
            foreach (string filePath in files) File.Delete(filePath);

            _dicManualUpdate.Clear();
        }

        public Dictionary<string, List<MUmain>> GetAllManualUpdates()
        {
            lock (_lockMU)
            {
                return _dicManualUpdate.ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value.Select(mu => mu.Clone()).ToList()
                );
            }
        }

        public bool LoadNewManualUpdate(string chartName, out List<MUmain> listMU)
        {
            var filePath = Path.Combine(_updateFilePath, $"{chartName}.mud");
            if (!File.Exists(filePath))
            {
                listMU = new();
                return false;
            }

            try
            {
                using var stream = File.OpenRead(filePath);
                var loadedData = JsonSerializer.Deserialize<List<MUmain>>(stream, _jsonOptions);
                if (loadedData != null)
                {
                    lock (_lockMU)
                    {
                        _dicManualUpdate[chartName] = loadedData;
                        listMU = loadedData.ToList();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"로드 실패: {ex.Message}");
            }

            listMU = new List<MUmain>();
            return false;
        }

        public bool LoadManualUpdate(string chartName, out List<MUmain> listMU)
        {
            lock (_lockMU)
            {
                if (_dicManualUpdate.TryGetValue(chartName, out var existingList))
                {
                    listMU = existingList.ToList();
                    return true;
                }
            }

            var filePath = Path.Combine(_updateFilePath, $"{chartName}.mud");
            if (!File.Exists(filePath))
            {
                listMU = new();
                return false;
            }

            try
            {
                using var stream = File.OpenRead(filePath);
                var loadedData = JsonSerializer.Deserialize<List<MUmain>>(stream, _jsonOptions);

                if (loadedData != null)
                {
                    lock (_lockMU)
                    {
                        // 그 사이에 다른 스레드가 로드했을 수도 있으니 다시 확인(TryAdd)
                        _dicManualUpdate.TryAdd(chartName, loadedData);
                        listMU = loadedData.ToList(); // 복사본 전달
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"로드 실패: {ex.Message}");
            }

            listMU = new List<MUmain>();
            return false; 
        }

        public bool SaveManualUpdate(string chartName)
        {
            List<MUmain> dataToSave = null;

            lock (_lockMU)
            {
                if (_dicManualUpdate.TryGetValue(chartName, out var list)) dataToSave = list.ToList();
            }

            if (dataToSave == null) return false;

            // 파일 경로 설정
            var filePath = Path.Combine(_updateFilePath, $"{chartName}.mud");
            var tempFilePath = filePath + ".tmp"; // 임시 파일 경로

            try
            {
                // 임시 파일(.tmp)에 먼저 생성.
                using (var stream = File.Create(tempFilePath))
                {
                    JsonSerializer.Serialize(stream, dataToSave, _jsonOptions);
                }

                // 쓰기가 완전히 성공했다면, 기존 파일을 삭제하고 임시 파일을 원본으로 교체합니다.
                if (File.Exists(filePath)) File.Delete(filePath);
                File.Move(tempFilePath, filePath);

                return true;
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                return false;
            }
        }

        public void AddManualUpdate(string chartName, MUmain mu)
        {
            lock (_lockMU)
            {
                if (mu.IsDelete == true) mu.RemoveDate = int.Parse(AppTime.Utc.ToString("yyyyMMdd"));
                else mu.RemoveDate = 0;

                if (_dicManualUpdate.TryGetValue(chartName, out var list) == true)
                {
                    mu.ID = list.Max(a => a.ID) + 1;
                    list.Add(mu);
                }
                else
                {
                    mu.ID = 1;
                    List<MUmain> listMU = new();
                    listMU.Add(mu);
                    _dicManualUpdate.TryAdd(chartName, listMU);
                }
            }

            SaveManualUpdate(chartName);
        }

        public void ModifyManualUpdate(string chartName, MUmain mu)
        {
            bool success = false;
            lock (_lockMU)
            {
                if (_dicManualUpdate.TryGetValue(chartName, out var list) == true)
                {
                    int index = list.FindIndex(a => a.ID == mu.ID);
                    if(index != -1)
                    {
                        list[index] = mu;
                        success = true;
                    }
                }
            }

            if(success) SaveManualUpdate(chartName);
        }

        public MUmain GetManualUpdateObject(string chartName, int id)
        {
            lock (_lockMU)
            {
                if (_dicManualUpdate.TryGetValue(chartName, out var list) == true)
                {
                    return list.Find(a => a.ID == id)?.Clone();
                }
            }

            return null;
        }

        public void UpdateManualUpdateObjectReview(string chartName, int id)
        {
            lock (_lockMU)
            {
                if (_dicManualUpdate.TryGetValue(chartName, out var list) == true)
                {
                    var mu = list.Where(a => a.ID == id).FirstOrDefault();
                    if(mu != null)
                    {
                        mu.IsReview = !mu.IsReview;
                    }
                }
            }
        }

        public void UpdateManualUpdateObjectDelete(string chartName, int id)
        {
            bool success = false;
            lock (_lockMU)
            {
                if (_dicManualUpdate.TryGetValue(chartName, out var list) == true)
                {
                    var mu = list.Where(a => a.ID == id).FirstOrDefault();
                    if (mu != null)
                    {
                        mu.IsDelete = !mu.IsDelete;
                        if (mu.IsDelete == true) mu.RemoveDate = int.Parse(AppTime.Utc.ToString("yyyyMMdd"));
                        else mu.RemoveDate = 0;
                        success = true;
                    }
                }
            }

            if(success) SaveManualUpdate(chartName);
        }

    }
}
