using Legacy.ECM_Core.Catalogue;
using Legacy.ECM_Core.Chart;
using Legacy.ECM_Core.Definition;
using Legacy.ECM_Core.Enumeration;
using System.IO;
using System.IO.Compression;

namespace Legacy.ECM_Core.Component
{
    public partial class ChartOrganizer
    {
        internal void Import_Chart(Dictionary<string, SearchChart> search_collection)
        {
            foreach (KeyValuePair<string, SearchChart> Search_Chart in search_collection)
            {
                DirectoryInfo Import_DirectoryInfo = new DirectoryInfo(Path.Combine(DirectoryDefinition.AppBase_Directory, DirectoryDefinition.ENC_Directory, Search_Chart.Key)); // 이쯤에서 .000이 import 되는 경우에는 폴더를 한 번 청소해주는 게 좋을 듯 (detection 단계에서 filtering이 된다고 해도, 기본적으로 혼재되는 구조면 긍정적이라고 볼 수는 없음)

                if (Import_DirectoryInfo.Exists)
                {
                    if (Search_Chart.Value.Cell.Where(Cell => Cell.File.Extension == ".000").Count() > 0)
                    {
                        IEnumerable<FileInfo> File_Enumeration = Import_DirectoryInfo.GetFiles().Where(File => !string.IsNullOrEmpty(File.Extension) && int.TryParse(File.Extension.Remove(0, 1), out int UPDN));

                        foreach (FileInfo File in File_Enumeration)
                        {
                            try
                            {
                                File.Delete();
                            }
                            catch (Exception e)
                            {

                            }
                        }
                    }
                }

                if (!Import_DirectoryInfo.Exists) { Import_DirectoryInfo.Create(); }

                FileInfo Boundary_FileInfo = new FileInfo(Path.Combine(Import_DirectoryInfo.FullName, "BOUNDARY"));

                if (Boundary_FileInfo.Exists)
                {
                    try
                    {
                        Boundary_FileInfo.Delete();
                    }
                    catch (Exception e)
                    {

                    }
                }

                using (StreamWriter Boundary_Writer = new StreamWriter(Boundary_FileInfo.FullName))
                {
                    (double North, double South, double East, double West) Boundary = Search_Chart.Value.Boundary;

                    Boundary_Writer.Write($"{Boundary.North},{Boundary.South},{Boundary.East},{Boundary.West}");
                    Boundary_Writer.Flush();
                }


                foreach (SearchCell Search_Cell in Search_Chart.Value.Cell)
                {
                    if (Search_Cell.File.Exists && CellCatalogue.Catalogue.ContainsKey(Search_Chart.Key))
                    {
                        List<ENC.Cell> Catalogue = CellCatalogue.Catalogue[Search_Chart.Key];
                        IEnumerable<ENC.Cell> Cell_Enumeration = Catalogue.Where(Cell => Path.GetFileName(Cell.File) == Search_Cell.File.Name);

                        if (Cell_Enumeration.Count() > 0)
                        {
                            if (ProductCatalogue.Catalogue.Count > 0)
                            {
                                ENC.Cell Cell = Cell_Enumeration.First();

                                if (!Extract_Cell(Cell, Search_Cell.File, Import_DirectoryInfo))
                                {
                                    
                                }
                            }
                            else
                            {
                                try
                                {
                                    FileInfo Import_File = new FileInfo(Path.Combine(Import_DirectoryInfo.FullName, Search_Cell.File.Name));

                                    File.Copy(Search_Cell.File.FullName, Import_File.FullName, true);

                                    // Extract_Cell에서 crc 체크 부분에 해당하는 것 추가
                                    byte[]? Cell_Data = File.ReadAllBytes(Import_File.FullName);
                                    ENC.Cell Cell = Cell_Enumeration.First();

                                    if (Cipher.CRC_32.Validate_CRC(Cell, Cell_Data))
                                    {

                                    }
                                    else
                                    {
                                        StandardError.Invoke_Message(SSE.ERROR_16, Search_Cell.File.Name);

                                        Import_File.Delete();
                                    }
                                }
                                catch (Exception e)
                                {

                                }
                            }
                        }


                        IEnumerable<ENC.Content> Content_Enumeration = CellCatalogue.Sub_Content.Where(Content => Path.GetDirectoryName(Content.Full_Name) == Path.GetDirectoryName(Search_Cell.File.FullName));

                        if (Content_Enumeration.Count() > 0)
                        {
                            foreach (ENC.Content Content in Content_Enumeration)
                            {
                                try
                                {
                                    File.Copy(Content.Full_Name, Path.Combine(Import_DirectoryInfo.FullName, Path.GetFileName(Content.Full_Name)), true);
                                }
                                catch (Exception e)
                                {

                                }
                            }
                        }
                    }
                }
            }
        }


        private bool Extract_Cell(ENC.Cell cell, FileInfo cell_file, DirectoryInfo import_directory)
        {
            bool Result = false;

            if (cell_file.Exists)
            {
                bool Compression = false;
                bool Encryption = false;

                IEnumerable<ENC.ProductRecord> ProductRecord_Enumeration = ProductCatalogue.Catalogue.Where(Catalogue => Catalogue.Record.ContainsKey(import_directory.Name)).Select(Catalogue => Catalogue.Record[import_directory.Name]);

                if (ProductRecord_Enumeration.Count() > 0)
                {
                    ENC.ProductRecord Product_Record = ProductRecord_Enumeration.First();

                    Compression = (Product_Record.Compression > 0);
                    Encryption = (Product_Record.Encryption > 0);
                }

                string Destination_Directory = Path.Combine(import_directory.FullName, "PRODUCT");
                string Destination = Path.Combine(Destination_Directory, cell_file.Name);

                if (!Directory.Exists(Destination_Directory))
                {
                    Directory.CreateDirectory(Destination_Directory);
                }

                try
                {
                    File.Copy(cell_file.FullName, Destination, true);
                }
                catch (Exception e)
                {
                    return false;
                }

                FileInfo Destination_File = new FileInfo(Destination);

                if (Destination_File.Exists)
                {
                    byte[]? Cell_Data = null;

                    if (Encryption)
                    {
                        try
                        {
                            Cell_Data = Decrypt_Cell(cell, Destination_File);
                        }
                        catch (Exception e)
                        {
                            Cell_Data = null;
                        }
                    }
                    else
                    {
                        try
                        {
                            Cell_Data = File.ReadAllBytes(Destination_File.FullName);
                        }
                        catch (Exception e)
                        {
                            Cell_Data = null;
                        }
                    }

                    if (Cell_Data != null)
                    {
                        FileInfo? Import_File = null;

                        if (Compression)
                        {
                            if (Uncompress_Cell(Cell_Data, import_directory))
                            {
                                Import_File = new FileInfo(Path.Combine(import_directory.FullName, cell_file.Name));
                            }
                            else
                            {
                                StandardError.Invoke_Message(SSE.ERROR_21, cell_file.Name);
                            }
                        }
                        else
                        {
                            FileInfo Writing_Cell = new FileInfo(Path.Combine(import_directory.FullName, cell_file.Name));

                            try
                            {
                                if (Writing_Cell.Exists) { Writing_Cell.Delete(); }

                                using (StreamWriter Cell_Writer = new StreamWriter(Writing_Cell.OpenWrite()))
                                {
                                    Cell_Writer.Write(Cell_Data);
                                    Cell_Writer.Flush();
                                }

                                Import_File = Writing_Cell;
                            }
                            catch (Exception e)
                            {
                                StandardError.Invoke_Message(SSE.ERROR_21, cell_file.Name);
                            }
                        }

                        if (Import_File != null)
                        {
                            if (Import_File.Exists)
                            {
                                try
                                {
                                    Cell_Data = File.ReadAllBytes(Import_File.FullName);
                                }
                                catch (Exception e)
                                {
                                    Cell_Data = null;

                                    StandardError.Invoke_Message(SSE.ERROR_21, cell_file.Name);
                                }

                                if (Cell_Data != null)
                                {
                                    if (Cipher.CRC_32.Validate_CRC(cell, Cell_Data))
                                    {
                                        // S57로 빼내는 기능?

                                        Result = true;
                                    }
                                    else
                                    {
                                        StandardError.Invoke_Message(SSE.ERROR_16, cell_file.Name);

                                        Import_File.Delete(); // 삭제 필요?
                                    }
                                }
                            }
                            else
                            {
                                StandardError.Invoke_Message(SSE.ERROR_21, cell_file.Name);
                            }
                        }
                    }
                    else
                    {
                        StandardError.Invoke_Message(SSE.ERROR_21, cell_file.Name);
                    }
                }
                else
                {
                    return false;
                }
            }

            return Result;
        }

        private byte[]? Decrypt_Cell(ENC.Cell cell, FileInfo cell_file)
        {
            string Chart_Name = Path.GetFileNameWithoutExtension(cell.File);

            if (PermitCatalogue.Catalogue.TryGetValue(Chart_Name, out List<ENC.CellPermit>? Cell_Permit) && (Cell_Permit != null))
            {
                IEnumerable<ENC.CellPermit> Permit_Enumeration = Cell_Permit.Where(Permit => Permit.DSID == cell.Provider);

                if (Permit_Enumeration.Count() > 0)
                {
                    ENC.CellPermit Permit = Permit_Enumeration.First();
                    string Key = (cell.EDTN > Permit.EDTN) ? Permit.Key.X : Permit.Key.Y;

                    Cipher.BlowFish.Initialize(Convert.FromHexString(Key));
                    
                    using (FileStream Encrypted_Stream = cell_file.OpenRead())
                    using (MemoryStream Decrypted_Stream = new MemoryStream())
                    {
                        while (true)
                        {
                            byte[] Read_Buffer = new byte[8];
                            int Read_Length = Encrypted_Stream.Read(Read_Buffer, 0, 8);

                            if (Read_Length > 0)
                            {
                                Decrypted_Stream.Write(Cipher.BlowFish.Decrypt(Read_Buffer, 0, 0), 0, 8);
                            }
                            else
                            {
                                break;
                            }
                        }

                        return Decrypted_Stream.ToArray();
                    }
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        private bool Uncompress_Cell(byte[] archive_data, DirectoryInfo import_directory)
        {
            bool Result = true;

            try
            {
                using (MemoryStream Archive_Stream = new MemoryStream(archive_data))
                using (ZipArchive Archive = new ZipArchive(Archive_Stream, ZipArchiveMode.Read))
                {
                    foreach (ZipArchiveEntry Archive_Entry in Archive.Entries)
                    {
                        try
                        {
                            Archive_Entry.ExtractToFile(Path.Combine(import_directory.FullName, Archive_Entry.FullName), true);
                        }
                        catch (Exception e)
                        {
                            Result = false;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Result = false;
            }
            
            return Result;
        }
    }
}