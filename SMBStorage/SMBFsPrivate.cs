//
// C# 
// TCFsSMBStorage
// v 0.1, 02.05.2024
// https://github.com/dkxce
// en,ru,1251,utf-8
//

using System;
using System.Collections.Generic;
using System.Text;
using TcPluginBase;
using TcPluginBase.Content;
using TcPluginBase.FileSystem;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using SMBLibrary.Client;
using SMBLibrary;
using dkxce.Crypt;

namespace SMBStorage
{
    public class SMBStorage
    {
        public string host = String.Empty;
        public int port = 445;
        public string user = "anonymous";
        public string pass = "";
        public string path = "";

        public string account
        {
            get
            {
                string isuser = string.IsNullOrEmpty(user) || user == "anonymous" ? "" : $"{user}@";
                string ispath = string.IsNullOrEmpty(path) ? "" : $"{path} on ";
                string isport = port == 445 ? "" : $":{port}";
                return $"{ispath}{isuser}{host}{isport}";
            }
        }

        public override string ToString() => full;

        public string full
        {
            get
            {
                return $"\\\\{user}:{pass}@{host}:{port}" + (string.IsNullOrEmpty(path) ? "" : $"\\{path}");
            }
        }
    }

    public class SMBStorages
    {
        public static string linkRegex = "^\\\\{0,2}((?<user>[^@:\\s\\\\]+)(:(?<pass>[^@:\\s\\\\]*))?@)?(?<host>[^@:\\s\\\\]+)(:(?<port>\\d{0,5}))?(?<path>\\\\.+)?$";
        private static string registry_key = "SOFTWARE\\dkxce\\TCFsSMBStorage\\Storages";
        private Dictionary<string, SMBStorage> storages = new Dictionary<string, SMBStorage> ();

        public SMBStorages()
        {
            try
            {
                RegistryKey rk = Registry.CurrentUser.OpenSubKey(registry_key, true);
                string[] vn = rk.GetValueNames();
                foreach (string v in vn)
                {
                    if (v == "") continue;
                    string kv = rk.GetValue(v, "").ToString();
                    if (string.IsNullOrEmpty(kv)) break;
                    kv = DIXU.DecryptText(kv, Base64Encode(v));
                    SMBStorage s = FromConnectionString(kv);
                    if (s != null) storages.Add(v, s);
                };
                rk.Close();
            }
            catch { };
        }

        public List<string> ListStorages()
        {
            List<string> result = new List<string>();
            foreach (KeyValuePair<string, SMBStorage> kvp in storages)
                result.Add(kvp.Key);
            return result;
        }

        private void SaveStorages()
        {
            try { RegistryKey rk = Registry.CurrentUser.CreateSubKey(registry_key, true); rk.Close(); } catch { };
            try
            {
                RegistryKey rk = Registry.CurrentUser.OpenSubKey(registry_key, true);
                string[] vn = rk.GetValueNames();
                foreach (string v in vn) if (v == "") continue; else rk.DeleteValue(v);
                foreach (KeyValuePair<string, SMBStorage> kvp in storages)
                {
                    string value = DIXU.EncryptText(kvp.Value.ToString(), Base64Encode(kvp.Key));
                    rk.SetValue(kvp.Key, value);
                };
                rk.Close();
            }
            catch { };
        }

        private static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public bool StorageExists(string name)
        {
            if(string.IsNullOrEmpty(name)) return false;
            foreach (KeyValuePair<string, SMBStorage> kvp in storages)
                if(name == kvp.Key) return true;
            return false;
        }

        public SMBStorage GetStorage(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            foreach (KeyValuePair<string, SMBStorage> kvp in storages)
                if (name == kvp.Key) return kvp.Value;
            return null;
        }

        public void DeleteStorage(string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            if (storages.ContainsKey(name))
            {
                storages.Remove(name);
                SaveStorages();
            };
        }

        public void AddStorage(string name, SMBStorage storage)
        {
            if (string.IsNullOrEmpty(name)) return;
            DeleteStorage(name);
            storages.Add(name, storage);
            SaveStorages();
        }

        public static SMBStorage FromConnectionString(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString)) return null;
            connectionString = connectionString.Trim().Trim(new char[] { '\\' });
            if (string.IsNullOrEmpty(connectionString)) return null;

            Regex rx = new Regex(linkRegex, RegexOptions.IgnoreCase);
            Match mx = rx.Match(connectionString);
            if (!mx.Success) return null;
            if (string.IsNullOrEmpty(mx.Groups["host"].Value)) return null;

            SMBStorage res = new SMBStorage() { host = mx.Groups["host"].Value };
            if (!string.IsNullOrEmpty(mx.Groups["pass"].Value)) res.pass = mx.Groups["pass"].Value;
            if (!string.IsNullOrEmpty(mx.Groups["user"].Value)) res.user = mx.Groups["user"].Value;
            if (!string.IsNullOrEmpty(mx.Groups["port"].Value)) res.port = int.Parse(mx.Groups["port"].Value);
            if (!string.IsNullOrEmpty(mx.Groups["path"].Value) && mx.Groups["path"].Value.Length > 0) res.path = mx.Groups["path"].Value.Substring(1);
            return res;
        }
    }

    public partial class SMBFs : FsPlugin
    {
        private static string registry_key = "SOFTWARE\\dkxce\\TCFsSMBStorage";
        private static string conn_failed = "Connection Failed";
        private static string plugin_name = "SMB 2.0+ Plugin";
        private static string help_name = "(help) F7 - add, edit or remove connection";

        public SMBStorages Storages = new SMBStorages();

        #region STORAGES'

        private IEnumerable<FindData> GetStorages()
        {
            foreach (string store in Storages.ListStorages())
                yield return new FindData(store, System.IO.FileAttributes.Directory | System.IO.FileAttributes.NoScrubData);

            yield return new FindData($"{help_name}.txt", 0, System.IO.FileAttributes.ReadOnly, DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow);
        }

        private bool CreateStorage(RemotePath path)
        {
            string ors = path.Segments[0] == help_name ? "" : path.Segments[0];
            EditStorageForm esf = new EditStorageForm() { OriginalStorageName = ors, Storages = Storages };
            SMBStorage si = Storages.GetStorage(ors);
            if (si != null) esf.DestinationLink = si.full; else esf.DestinationLink = ors;
            esf.UpdateNames();
            esf.ShowDialog();
            if (esf.Result == "delete")
            {
                if (MessageBox.Show($"Do you want to delete {ors} storage?", Title, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    Storages.DeleteStorage(ors);
                    MessageBox.Show($"Storage {ors} successfully deleted", Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    esf.Dispose();
                    return true;
                }
                else
                {
                    esf.Dispose();
                    return false;
                };
            };
            if (esf.Result == "ok")
            {
                if (!Storages.StorageExists(ors))
                {
                    if (Storages.StorageExists(esf.DestinationStorageName))
                    {
                        if (MessageBox.Show($"Storage {esf.DestinationStorageName} already exists!\r\nRewrite it?", Title, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                        {
                            Storages.AddStorage(esf.DestinationStorageName, SMBStorages.FromConnectionString(esf.DestinationLink));
                            esf.Dispose();
                            return true;
                        }
                        else
                        {
                            esf.Dispose();
                            return false;
                        };
                    }
                    else
                    {
                        Storages.AddStorage(esf.DestinationStorageName, SMBStorages.FromConnectionString(esf.DestinationLink));
                        esf.Dispose();
                        return true;
                    };
                }
                else
                {
                    if (Storages.StorageExists(esf.DestinationStorageName))
                    {
                        if (ors != esf.DestinationStorageName)
                        {
                            MessageBox.Show($"Storage {esf.DestinationStorageName} already exists!", Title, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                            esf.Dispose();
                            return false;
                        }
                        else
                        {
                            MessageBox.Show($"Storage {esf.DestinationStorageName} updated successfully!", Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                            Storages.AddStorage(esf.DestinationStorageName, SMBStorages.FromConnectionString(esf.DestinationLink));
                            esf.Dispose();
                            return true;
                        };
                    }
                    else
                    {
                        MessageBoxManager.Yes = "Rename";
                        MessageBoxManager.No  = "Create New";
                        MessageBoxManager.Register();
                        DialogResult dr = MessageBox.Show($"Rename {ors} storage to {esf.DestinationStorageName}?", Title, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                        MessageBoxManager.Unregister();
                        if ( dr == DialogResult.Yes)
                        {
                            Storages.DeleteStorage(ors);
                            Storages.AddStorage(esf.DestinationStorageName, SMBStorages.FromConnectionString(esf.DestinationLink));
                            esf.Dispose();
                            return true;
                        }
                        else if (dr == DialogResult.No)
                        {
                            Storages.AddStorage(esf.DestinationStorageName, SMBStorages.FromConnectionString(esf.DestinationLink));
                            esf.Dispose();
                            return true;
                        }
                        else
                        {
                            esf.Dispose();
                            return false;
                        };
                    };    
                };
            };
            esf.Dispose();
            return false;
        }        

        private bool DeleteAccount(RemotePath path)
        {
            SMBStorage s = Storages.GetStorage(path.Segments[0]);
            if (s == null) return false;
            string p = path.Level > 1 ? path.Path.Trim('\\').Split(new char[] { '\\' }, 2)[1] : "";
            if (string.IsNullOrEmpty(p))
            {
                Storages.DeleteStorage(path.Segments[0]);
                MessageBox.Show($"Storage {path.Segments[0]} successfully deleted", Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return true;
            };
            return false;
        }

        #endregion STORAGES        

        #region PATHS

        public IEnumerable<FindData> GetPathFiles(RemotePath path)
        {

            SMBStorage s = Storages.GetStorage(path.Segments[0]);
            if (s == null) return null;
            string p = path.Level > 1 ? path.Path.Trim('\\').Split(new char[] { '\\' }, 2)[1] : "";
            return GetStorageFiles(s, p);
        }

        private bool ConnectToStorage(SMBStorage s, ref string path, out SMB2Client client)
        {
            //SMB1Client s1 = new SMB1Client();
            //SMB1Client.DirectTCPPort = s.port;
            //s1 = new SMB1Client();
            //if (!string.IsNullOrEmpty(s.path)) path = (s.path.Trim('\\') + "\\" + (string.IsNullOrEmpty(path) ? "" : path)).Trim('\\');
            //bool c = s1.Connect(s.host, SMBTransportType.DirectTCPTransport);
            //MessageBox.Show($"{c}");

            SMB2Client.DirectTCPPort = s.port;
            client = new SMB2Client();
            if (!string.IsNullOrEmpty(s.path)) path = (s.path.Trim('\\') + "\\" + (string.IsNullOrEmpty(path) ? "" : path)).Trim('\\');
            return client.Connect(s.host, SMBTransportType.DirectTCPTransport);
        }

        private ISMBFileStore SMBTreeConnect(SMB2Client client, ref string path, out string directoryPath, out NTStatus status)
        {
            string[] splitted_path = path.Split(new char[] { '\\' }, 2);
            ISMBFileStore fileStore = client.TreeConnect(splitted_path[0], out status);
            directoryPath = String.Empty;
            if (splitted_path.Length > 1) directoryPath = splitted_path[1].Trim('\\');
            return fileStore;
        }

        public IEnumerable<FindData> GetStorageFiles(SMBStorage s, string path)
        {
            List<FindData> result = new List<FindData>();
            
            bool connected = false;
            Exception error = null;
            SMB2Client client = null;
            try { connected = ConnectToStorage(s, ref path, out client); } catch (Exception e) { error = e; };
            if (connected)
            {
                yield return new FindData("..", System.IO.FileAttributes.Directory);

                if (string.IsNullOrEmpty(path))
                {
                    if (client.Login(String.Empty, s.user, s.pass) == NTStatus.STATUS_SUCCESS)
                    {
                        List<string> shares = client.ListShares(out _);
                        foreach (string sh in shares)
                            yield return new FindData(sh, 0, System.IO.FileAttributes.Directory);
                        client.Logoff();
                    };                    
                }
                else
                {
                    NTStatus status = client.Login(String.Empty, s.user, s.pass);
                    if (status == NTStatus.STATUS_SUCCESS)
                    {
                        ISMBFileStore fileStore = SMBTreeConnect(client, ref path, out string dPath, out status);
                        if (status == NTStatus.STATUS_SUCCESS)
                        {
                            FileStatus fileStatus;
                            status = fileStore.CreateFile(out object directoryHandle, out fileStatus, dPath, AccessMask.GENERIC_READ, SMBLibrary.FileAttributes.Directory, ShareAccess.Read | ShareAccess.Write, CreateDisposition.FILE_OPEN, CreateOptions.FILE_DIRECTORY_FILE, null);
                            if (status == NTStatus.STATUS_SUCCESS)
                            {
                                List<QueryDirectoryFileInformation> fileList;
                                fileStore.QueryDirectory(out fileList, directoryHandle, "*", FileInformationClass.FileDirectoryInformation);
                                fileStore.CloseFile(directoryHandle);
                                foreach (FileDirectoryInformation file in fileList)
                                {
                                    if (file.FileName == "..") continue;
                                    bool isDir = (file.FileAttributes & SMBLibrary.FileAttributes.Directory) == SMBLibrary.FileAttributes.Directory;
                                    yield return new FindData(file.FileName, (ulong)(file.EndOfFile), 
                                        isDir ? System.IO.FileAttributes.Directory : System.IO.FileAttributes.Normal, file.LastWriteTime, file.CreationTime, file.LastAccessTime);                                    
                                }
                            };                            
                        };
                        try { fileStore.Disconnect(); } catch { };
                        client.Logoff();
                    };                    
                };
                try { client.Disconnect(); } catch { };
            }
            else
            {
                if (error != null) MessageBox.Show(error.Message, Title, MessageBoxButtons.OK, MessageBoxIcon.Error);
                yield return new FindData("..", System.IO.FileAttributes.Directory);
                yield return new FindData(conn_failed, System.IO.FileAttributes.Normal | System.IO.FileAttributes.ReadOnly);
            };
        }

        private bool CreateDirectory(RemotePath path)
        {
            SMBStorage s = Storages.GetStorage(path.Segments[0]);
            if (s == null) return false;

            string p = path.Level > 1 ? path.Path.Trim('\\').Split(new char[] { '\\' }, 2)[1] : "";
            if (ConnectToStorage(s, ref p, out SMB2Client client))
            {
                bool created = false;
                NTStatus status = client.Login(String.Empty, s.user, s.pass);
                if (status == NTStatus.STATUS_SUCCESS)
                {
                    ISMBFileStore fileStore = SMBTreeConnect(client, ref p, out string directoryPath, out status);
                    if (status == NTStatus.STATUS_SUCCESS)
                    {
                        FileStatus fileStatus;
                        status = fileStore.CreateFile(out object directoryHandle, out fileStatus, directoryPath, AccessMask.GENERIC_WRITE, SMBLibrary.FileAttributes.Directory, ShareAccess.Read | ShareAccess.Write, CreateDisposition.FILE_CREATE, CreateOptions.FILE_DIRECTORY_FILE, null);
                        if (status == NTStatus.STATUS_SUCCESS)
                        {
                            fileStore.CloseFile(directoryHandle);
                            created = true;
                        };                        
                    };
                    try { fileStore.Disconnect(); } catch { };
                    client.Logoff();
                };
                try { client.Disconnect(); } catch { };
                return created;
            };

            return false;
        }

        private bool DeletePath(RemotePath path)
        {
            SMBStorage s = Storages.GetStorage(path.Segments[0]);
            if (s == null) return false;

            string p = path.Level > 1 ? path.Path.Trim('\\').Split(new char[] { '\\' }, 2)[1] : "";
            if (ConnectToStorage(s, ref p, out SMB2Client client))
            {
                bool deleted = false;
                NTStatus status = client.Login(String.Empty, s.user, s.pass);
                if (status == NTStatus.STATUS_SUCCESS)
                {
                    ISMBFileStore fileStore = SMBTreeConnect(client, ref p, out string directoryPath, out status);
                    if (status == NTStatus.STATUS_SUCCESS)
                    {
                        FileStatus fileStatus;
                        status = fileStore.CreateFile(out object directoryHandle, out fileStatus, directoryPath, AccessMask.DELETE, SMBLibrary.FileAttributes.Directory, ShareAccess.Delete, CreateDisposition.FILE_OPEN, CreateOptions.FILE_DELETE_ON_CLOSE, null);
                        if (status == NTStatus.STATUS_SUCCESS)
                        {
                            fileStore.CloseFile(directoryHandle);
                            deleted = true;
                        };                        
                    };
                    try { fileStore.Disconnect(); } catch { };
                    client.Logoff();
                };
                try { client.Disconnect(); } catch { };
                return deleted;
            };

            return false;
        }

        private bool DeletePathFile(RemotePath path)
        {
            SMBStorage s = Storages.GetStorage(path.Segments[0]);
            if (s == null) return false;

            string p = path.Level > 1 ? path.Path.Trim('\\').Split(new char[] { '\\' }, 2)[1] : "";
            if (p == conn_failed) return true;

            if (ConnectToStorage(s, ref p, out SMB2Client client))
            {
                bool deleted = false;
                NTStatus status = client.Login(String.Empty, s.user, s.pass);
                if (status == NTStatus.STATUS_SUCCESS)
                {
                    ISMBFileStore fileStore = SMBTreeConnect(client, ref p, out string directoryPath, out status);
                    if (status == NTStatus.STATUS_SUCCESS)
                    {
                        FileStatus fileStatus;
                        status = fileStore.CreateFile(out object directoryHandle, out fileStatus, directoryPath, AccessMask.DELETE, SMBLibrary.FileAttributes.Normal, ShareAccess.Delete, CreateDisposition.FILE_OPEN, CreateOptions.FILE_DELETE_ON_CLOSE, null);
                        if (status == NTStatus.STATUS_SUCCESS)
                        {
                            fileStore.CloseFile(directoryHandle);
                            deleted = true;
                        };                        
                    };
                    try { fileStore.Disconnect(); } catch { };
                    client.Logoff();
                };
                try { client.Disconnect(); } catch { };
                return deleted;
            };

            return false;
        }

        private async Task<FileSystemExitCode> DownloadFile(RemotePath remoteName, string localName, bool performMove, Action<int> setProgress, CancellationToken token)
        {
            SMBStorage s = Storages.GetStorage(remoteName.Segments[0]);
            if (s == null) return FileSystemExitCode.FileNotFound;

            string p = remoteName.Level > 1 ? remoteName.Path.Trim('\\').Split(new char[] { '\\' }, 2)[1] : "";
            if (string.IsNullOrEmpty(p)) return FileSystemExitCode.FileNotFound;

            int prevPercent = -1;

            if (ConnectToStorage(s, ref p, out SMB2Client client))
            {
                NTStatus status = client.Login(String.Empty, s.user, s.pass);
                if (status == NTStatus.STATUS_SUCCESS)
                {
                    string[] pp = p.Split(new char[] { '\\' }, 2);
                    ISMBFileStore fileStore = client.TreeConnect(pp[0], out status);
                    string filePath = pp[1].Trim('\\');
                    if (fileStore is SMB1FileStore) filePath = @"\\" + filePath;

                    FileStatus fileStatus;
                    long fileSize = 0;
                    // GET FILE SIZE //
                    {
                        if (pp.Length > 1)
                        {
                            string fileN;
                            string dPath = pp[1].Trim('\\');
                            int lof = dPath.LastIndexOf("\\");
                            if (lof > 0)
                            {
                                fileN = dPath.Substring(lof).Trim('\\');
                                dPath = dPath.Substring(0, lof);
                            }
                            else
                            {
                                fileN = dPath;
                                dPath = String.Empty;
                            };
                            status = fileStore.CreateFile(out object directoryHandle, out fileStatus, dPath, AccessMask.GENERIC_READ, SMBLibrary.FileAttributes.Directory, ShareAccess.Read | ShareAccess.Write, CreateDisposition.FILE_OPEN, CreateOptions.FILE_DIRECTORY_FILE, null);
                            if (status == NTStatus.STATUS_SUCCESS)
                            {
                                List<QueryDirectoryFileInformation> fileList;
                                fileStore.QueryDirectory(out fileList, directoryHandle, fileN, FileInformationClass.FileDirectoryInformation);
                                fileStore.CloseFile(directoryHandle);
                                foreach (FileDirectoryInformation file in fileList) fileSize = (file.EndOfFile);
                            };
                        };
                    };
                    // COOPY FILE //
                    {
                        status = fileStore.CreateFile(out object fileHandle, out fileStatus, filePath, AccessMask.GENERIC_READ | AccessMask.SYNCHRONIZE, SMBLibrary.FileAttributes.Normal, ShareAccess.Read, CreateDisposition.FILE_OPEN, CreateOptions.FILE_NON_DIRECTORY_FILE | CreateOptions.FILE_SYNCHRONOUS_IO_ALERT, null);
                        if (status == NTStatus.STATUS_SUCCESS)
                        {
                            System.IO.FileStream stream = null;
                            try { stream = new System.IO.FileStream(localName, FileMode.Create, FileAccess.Write); }
                            catch
                            {
                                fileStore.CloseFile(fileHandle);
                                try { fileStore.Disconnect(); } catch { };
                                client.Logoff();
                                return FileSystemExitCode.WriteError;
                            };
                            byte[] data;
                            long bytesRead = 0;
                            while (true)
                            {
                                status = fileStore.ReadFile(out data, fileHandle, bytesRead, (int)client.MaxReadSize);
                                if (status != NTStatus.STATUS_SUCCESS && status != NTStatus.STATUS_END_OF_FILE)
                                {
                                    stream.Close();
                                    fileStore.CloseFile(fileHandle);
                                    try { fileStore.Disconnect(); } catch { };
                                    client.Logoff();
                                    return FileSystemExitCode.ReadError;
                                };
                                if (status == NTStatus.STATUS_END_OF_FILE || data.Length == 0)
                                    break;
                                if (token.IsCancellationRequested)
                                {
                                    stream.Close();
                                    fileStore.CloseFile(fileHandle);
                                    try { fileStore.Disconnect(); } catch { };
                                    client.Logoff();
                                    return FileSystemExitCode.UserAbort;
                                };
                                bytesRead += data.Length;
                                int percent = (int)((float)bytesRead / (float)fileSize * 100.0);
                                stream.Write(data, 0, data.Length);
                                if (percent != prevPercent)
                                {
                                    prevPercent = percent;
                                    setProgress(percent);
                                };
                            };
                            stream.Close();
                            fileStore.CloseFile(fileHandle);
                        };
                    };
                    try { fileStore.Disconnect(); } catch { };
                };
                try { client.Disconnect(); } catch { };
                try { if (performMove) DeletePathFile(remoteName); } catch { };
                return FileSystemExitCode.OK;
            };            
            return FileSystemExitCode.WriteError;
        }

        private async Task<FileSystemExitCode> UploadFile(RemotePath remoteName, string localName, bool overwrite, Action<int> setProgress, CancellationToken token)
        {
            SMBStorage s = Storages.GetStorage(remoteName.Segments[0]);
            if (s == null) return FileSystemExitCode.FileNotFound;

            string p = remoteName.Level > 1 ? remoteName.Path.Trim('\\').Split(new char[] { '\\' }, 2)[1] : "";
            if (string.IsNullOrEmpty(p)) return FileSystemExitCode.FileNotFound;

            int prevPercent = -1;

            if (ConnectToStorage(s, ref p, out SMB2Client client))
            {
                NTStatus status = client.Login(String.Empty, s.user, s.pass);
                if (status == NTStatus.STATUS_SUCCESS)
                {
                    string[] pp = p.Split(new char[] { '\\' }, 2);
                    ISMBFileStore fileStore = client.TreeConnect(pp[0], out status);
                    string filePath = pp[1].Trim('\\');
                    if (fileStore is SMB1FileStore) filePath = @"\\" + filePath;

                    FileStatus fileStatus;

                    // CHECK FILE EXISTS //
                    if(!overwrite)
                    {
                        if (pp.Length > 1)
                        {
                            string fileN;
                            string dPath = pp[1].Trim('\\');
                            int lof = dPath.LastIndexOf("\\");
                            if (lof > 0)
                            {
                                fileN = dPath.Substring(lof).Trim('\\');
                                dPath = dPath.Substring(0, lof);
                            }
                            else
                            {
                                fileN = dPath;
                                dPath = String.Empty;
                            };
                            status = fileStore.CreateFile(out object directoryHandle, out fileStatus, dPath, AccessMask.GENERIC_READ, SMBLibrary.FileAttributes.Directory, ShareAccess.Read | ShareAccess.Write, CreateDisposition.FILE_OPEN, CreateOptions.FILE_DIRECTORY_FILE, null);
                            if (status == NTStatus.STATUS_SUCCESS)
                            {
                                List<QueryDirectoryFileInformation> fileList;
                                fileStore.QueryDirectory(out fileList, directoryHandle, fileN, FileInformationClass.FileDirectoryInformation);
                                fileStore.CloseFile(directoryHandle);
                                foreach (FileDirectoryInformation file in fileList)
                                    return FileSystemExitCode.FileExists;
                            };
                        };
                    };

                    // COPY FILE //
                    {
                        System.IO.FileStream stream = null;
                        try { stream = new System.IO.FileStream(localName, FileMode.Open, FileAccess.Read, FileShare.Read); }
                        catch
                        {
                            try { fileStore.Disconnect(); } catch { };
                            client.Logoff();
                            return FileSystemExitCode.ReadError;
                        };
                        status = fileStore.CreateFile(out object fileHandle, out fileStatus, filePath, AccessMask.GENERIC_WRITE | AccessMask.SYNCHRONIZE, SMBLibrary.FileAttributes.Normal, ShareAccess.None, CreateDisposition.FILE_CREATE, CreateOptions.FILE_NON_DIRECTORY_FILE | CreateOptions.FILE_SYNCHRONOUS_IO_ALERT, null);
                        if (status == NTStatus.STATUS_SUCCESS)
                        {
                            long bytesWrote = 0;
                            byte[] data = new byte[client.MaxWriteSize];
                            int readed = 0;
                            while ((readed = stream.Read(data, 0, data.Length)) > 0)
                            {
                                byte[] toWrite;
                                if (readed != data.Length)
                                {
                                    toWrite = new byte[readed];
                                    Array.Copy(data, 0, toWrite, 0, readed);
                                }
                                else
                                {
                                    toWrite = data;
                                };
                                status = fileStore.WriteFile(out int numberOfBytesWritten, fileHandle, bytesWrote, toWrite);
                                bytesWrote += numberOfBytesWritten;

                                if (status != NTStatus.STATUS_SUCCESS || numberOfBytesWritten != readed)
                                {
                                    fileStore.CloseFile(fileHandle);
                                    stream.Close();
                                    try { fileStore.Disconnect(); } catch { };
                                    client.Logoff();
                                    return FileSystemExitCode.WriteError;
                                };

                                if (token.IsCancellationRequested)
                                {
                                    fileStore.CloseFile(fileHandle);
                                    stream.Close();
                                    try { fileStore.Disconnect(); } catch { };
                                    client.Logoff();
                                    return FileSystemExitCode.UserAbort;
                                };

                                int percent = (int)((float)bytesWrote / (float)stream.Length * 100.0);
                                if (percent != prevPercent)
                                {
                                    prevPercent = percent;
                                    setProgress(percent);
                                };
                            };
                            fileStore.CloseFile(fileHandle);
                        };
                        stream.Close();
                    };
                    try { fileStore.Disconnect(); } catch { };
                };
                try { client.Disconnect(); } catch { };
                return FileSystemExitCode.OK;
            };
            return FileSystemExitCode.WriteError;
        }        

        #endregion PATHS

        #region HELP        

        public static string CurrentDirectory()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
            // return Application.StartupPath;
            // return System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            // return System.IO.Directory.GetCurrentDirectory();
            // return Environment.CurrentDirectory;
            // return System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);
            // return System.IO.Path.GetDirectory(Application.ExecutablePath);
        }

        #endregion HELP
    }
}
