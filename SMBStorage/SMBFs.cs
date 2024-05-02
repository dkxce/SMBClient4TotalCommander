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
using System.Threading.Tasks;
using TcPluginBase;
using TcPluginBase.Content;
using TcPluginBase.FileSystem;
using System.Windows.Forms;
using System.Threading;
using System.IO;


namespace SMBStorage
{    
    public partial class SMBFs: FsPlugin
    {
        private readonly Settings _pluginSettings;        

        public SMBFs(Settings pluginSettings) : base(pluginSettings)
        {
            _pluginSettings = pluginSettings;
            Title = plugin_name;

            TcPluginEventHandler += (sender, args) => 
            {
                switch (args)
                {
                    case RequestEventArgs x:
                        Log.Info($"Event: {args.GetType().Name}: CustomTitle: {x.CustomTitle}");
                        break;
                    case ProgressEventArgs x:
                        Log.Info($"Event: {args.GetType().Name}: PercentDone: {x.PercentDone}");
                        break;
                    case ContentProgressEventArgs x:
                        Log.Info($"Event: {args.GetType().Name}: NextBlockData: {x.NextBlockData}");
                        break;
                    default:
                        Log.Info($"Event: {args.GetType().FullName}");
                        break;
                }
            };
        }

        #region IFsPlugin Members

        public override IEnumerable<FindData> GetFiles(RemotePath path)
        {
            path.Path = new RemotePath(@"\" + path.Path.TrimStart('\\'));            
            if (path.Level == 0) return GetStorages();
            else return GetPathFiles(path);
        }
        
        public override bool MkDir(RemotePath path)
        {
            path.Path = new RemotePath(@"\" + path.Path.TrimStart('\\'));
            if (path.Level == 0) return false;
            else if (path.Level == 1) return CreateStorage(path);
            else return CreateDirectory(path);
        }

        public override bool RemoveDir(RemotePath path)
        {
            path.Path = new RemotePath(@"\" + path.Path.TrimStart('\\'));            
            if (path.Level == 0) return false;
            else if (path.Level == 1) return DeleteAccount(path);
            else return DeletePath(path);
        }

        public override bool DeleteFile(RemotePath path)
        {
            path.Path = new RemotePath(@"\" + path.Path.TrimStart('\\'));
            if (path.Level > 1) return DeletePathFile(path);
            else return false;
        }
        
        public override async Task<FileSystemExitCode> PutFileAsync(string localName, RemotePath remoteName, CopyFlags copyFlags, Action<int> setProgress, CancellationToken token)
        {
            remoteName.Path = new RemotePath(@"\" + remoteName.Path.TrimStart('\\'));
            
            bool overWrite = (CopyFlags.Overwrite & copyFlags) != 0;
            bool performMove = (CopyFlags.Move & copyFlags) != 0;
            bool resume = (CopyFlags.Resume & copyFlags) != 0;
            if (resume) return FileSystemExitCode.NotSupported;
            if (!System.IO.File.Exists(localName)) return FileSystemExitCode.FileNotFound;                        

            FileSystemExitCode result = await UploadFile(remoteName, localName, overWrite, setProgress, token);
            if (performMove) System.IO.File.Delete(localName);
            return result;
        }        

        public override async Task<FileSystemExitCode> GetFileAsync(RemotePath remoteName, string localName, CopyFlags copyFlags, RemoteInfo remoteInfo, Action<int> setProgress, CancellationToken token)
        {
            remoteName.Path = new RemotePath(@"\" + remoteName.Path.TrimStart('\\'));

            bool overWrite = (CopyFlags.Overwrite & copyFlags) != 0;
            bool performMove = (CopyFlags.Move & copyFlags) != 0;
            bool resume = (CopyFlags.Resume & copyFlags) != 0;
            if (resume) return FileSystemExitCode.NotSupported;            
            if (System.IO.File.Exists(localName) && !overWrite) return FileSystemExitCode.FileExists;

            return await DownloadFile(remoteName, localName, performMove, setProgress, token);
        }

        public override ExecResult ExecuteCommand(TcWindow mainWin, RemotePath remoteName, string command)
        {
            switch (command)
            {
                case "refresh":
                    mainWin.Refresh();
                    return ExecResult.Ok;
                default:
                    Log.Info($"{nameof(ExecuteCommand)}(\"{mainWin.Handle}\", \"{remoteName}\", \"{command}\")");
                    return ExecResult.Yourself;
            };
        }

        #endregion IFsPlugin Members

        #region NOT SUPPORTED OR STANDARD

        public override FileSystemExitCode RenMovFile(RemotePath oldName, RemotePath newName, bool move, bool overwrite, RemoteInfo remoteInfo) => FileSystemExitCode.NotSupported;        

        public override ExtractIconResult ExtractCustomIcon(RemotePath remoteName, ExtractIconFlags extractFlags) => ExtractIconResult.UseDefault;

        public override void StatusInfo(string remoteDir, InfoStartEnd startEnd, InfoOperation infoOperation) => base.StatusInfo(remoteDir, startEnd, infoOperation);

        public override ExecResult ExecuteProperties(TcWindow mainWin, RemotePath remoteName) => ExecResult.Yourself;

        public override ExecResult ExecuteOpen(TcWindow mainWin, RemotePath remoteName) => ExecResult.Yourself;

        #endregion NOT SUPPORTED OR STANDARD
    }
}
