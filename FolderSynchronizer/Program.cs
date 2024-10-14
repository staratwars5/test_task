using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Timer = System.Timers.Timer;

namespace FolderSynchronizer
{
  class Program
  {
    private static string sourceFolder;
    private static string replicaFolder;
    private static string logFilePath;
    private static Timer syncTimer;

    static void Main(string[] args)
    {
      // Command-line arguments: sourceFolder, replicaFolder, interval, logFilePath
      if (args.Length < 4)
      {
        Console.WriteLine("Usage: FolderSynchronizer <sourceFolder> <replicaFolder> <intervalInSeconds> <logFilePath>");
        return;
      }

      sourceFolder = args[0];
      replicaFolder = args[1];
      int intervalInSeconds = int.Parse(args[2]);
      logFilePath = args[3];

      // Set up the periodic synchronization using a Timer
      syncTimer = new Timer(intervalInSeconds * 1000);
      syncTimer.Elapsed += (sender, e) => SynchronizeFolders();
      syncTimer.Start();

      Console.WriteLine($"Synchronization started. Syncing every {intervalInSeconds} seconds.");
      Console.ReadLine();
    }

    static void SynchronizeFolders()
    {
      try
      {
        Log("Synchronization started.");
        SyncDirectory(sourceFolder, replicaFolder);
        Log("Synchronization completed.");
      }
      catch (Exception ex)
      {
        Log($"Error during synchronization: {ex.Message}");
      }
    }

    static void SyncDirectory(string source, string target)
    {
      // Create the target directory if it does not exist
      if (!Directory.Exists(target))
      {
        Directory.CreateDirectory(target);
        Log($"Created directory: {target}");
      }

      // Sync all files in the source directory
      foreach (var sourceFile in Directory.GetFiles(source))
      {
        var targetFile = Path.Combine(target, Path.GetFileName(sourceFile));
        if (!File.Exists(targetFile) || !FilesAreEqual(sourceFile, targetFile))
        {
          File.Copy(sourceFile, targetFile, true);
          Log($"Copied/Updated file: {sourceFile} -> {targetFile}");
        }
      }

      // Recursively sync subdirectories
      foreach (var sourceSubDir in Directory.GetDirectories(source))
      {
        var targetSubDir = Path.Combine(target, Path.GetFileName(sourceSubDir));
        SyncDirectory(sourceSubDir, targetSubDir);
      }

      // Delete files in the target that are not in the source
      foreach (var targetFile in Directory.GetFiles(target))
      {
        var sourceFile = Path.Combine(source, Path.GetFileName(targetFile));
        if (!File.Exists(sourceFile))
        {
          File.Delete(targetFile);
          Log($"Deleted file: {targetFile}");
        }
      }

      // Delete directories in the target that are not in the source
      foreach (var targetSubDir in Directory.GetDirectories(target))
      {
        var sourceSubDir = Path.Combine(source, Path.GetFileName(targetSubDir));
        if (!Directory.Exists(sourceSubDir))
        {
          Directory.Delete(targetSubDir, true);
          Log($"Deleted directory: {targetSubDir}");
        }
      }
    }

    static bool FilesAreEqual(string file1, string file2)
    {
      // Compare files using MD5 checksum
      using (var md5 = MD5.Create())
      {
        byte[] file1Bytes = File.ReadAllBytes(file1);
        byte[] file2Bytes = File.ReadAllBytes(file2);
        byte[] file1Hash = md5.ComputeHash(file1Bytes);
        byte[] file2Hash = md5.ComputeHash(file2Bytes);
        return file1Hash.SequenceEqual(file2Hash);
      }
    }

    static void Log(string message)
    {
      var logMessage = $"{DateTime.Now}: {message}";
      Console.WriteLine(logMessage);
      File.AppendAllText(logFilePath, logMessage + Environment.NewLine);
    }
  }
}
