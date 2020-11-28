using BlazorPlus;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace WatchTemplate.Services
{
    public enum ChangeTypes
    {
        Removed,
        Added,
        Unmodified,
        Modified
    }

    public record FileStatus(string Path, ChangeTypes State);

    public class Diff
    {
        public List<BlazorTreeNode> Nodes { get; }

        public TemplateConverter.DiffRequestData DiffRequestData { get; }

        public Diff(Diff oldDiff, TemplateConverter.DiffRequestData diffRequestData)
        {
            DiffRequestData = diffRequestData;
            var originalFiles = Directory.GetFileSystemEntries(diffRequestData.LeftRequestData.OutputPath, "*", SearchOption.AllDirectories).Select(p => p.Substring(diffRequestData.LeftRequestData.OutputPath.Length + 1)).ToArray();
            var changedFiles = Directory.GetFileSystemEntries(diffRequestData.RightRequestData.OutputPath, "*", SearchOption.AllDirectories).Select(p => p.Substring(diffRequestData.RightRequestData.OutputPath.Length + 1)).ToArray();

            var removed = originalFiles.Except(changedFiles);
            var added = changedFiles.Except(originalFiles);
            var common = originalFiles.Intersect(changedFiles);
            var modified = common.Where(s => !AreFilesSame(diffRequestData.LeftRequestData.OutputPath, diffRequestData.RightRequestData.OutputPath, s)).ToArray();
            common = common.Except(modified);

            var files = removed.Select(p => new FileStatus(p, ChangeTypes.Removed))
                .Concat(added.Select(p => new FileStatus(p, ChangeTypes.Added)))
                .Concat(common.Select(p => new FileStatus(p, ChangeTypes.Unmodified)))
                .Concat(modified.Select(p => new FileStatus(p, ChangeTypes.Modified)))
                .OrderBy(f => f.Path)
                .ToArray();

            var rootNode = new BlazorTreeNode();
            foreach (var (path, status) in files)
            {
                var currentNode = rootNode;
                var partsOfPath = path.Split(Path.DirectorySeparatorChar);
                for (int i = 0; i < partsOfPath.Length; i++)
                {
                    string subPath = partsOfPath[i];
                    var childNode = currentNode.Nodes.FirstOrDefault((n) => n.Text == subPath);
                    if (childNode == null)
                    {
                        childNode = new BlazorTreeNode() { Text = subPath, DateItem = (path, status), NodeCssText=$"color:{GetChangeColor(status)};" };
                        currentNode.Nodes.Add(childNode);
                    }
                    currentNode = childNode;
                }
            }

            Nodes = rootNode.Nodes.ToList();
            if (oldDiff is null)
            {
                var toProcess = new Queue<BlazorTreeNode>(Nodes);
                while (toProcess.Count > 0)
                {
                    var item = toProcess.Dequeue();
                    if (item.NodeCount > 0)
                        item.IsExpanded = true;
                    foreach (var node in item.Nodes)
                    {
                        toProcess.Enqueue(node);
                    }
                }
            }
            else
            {
                SyncNodes(oldDiff.Nodes, Nodes);
            }
        }

         string GetChangeColor(ChangeTypes status)
        {
            switch (status)
            {
                case ChangeTypes.Removed:
                    return "red";
                case ChangeTypes.Added:
                    return "green";
                case ChangeTypes.Unmodified:
                    return "black";
                case ChangeTypes.Modified:
                    return "orange";
                default:
                    throw new NotImplementedException();
            }
        }

        void SyncNodes(List<BlazorTreeNode> old, List<BlazorTreeNode> current)
        {
            foreach (var node in current)
            {
                if (node.Nodes.Count == 0)
                    continue;
                var o = old.FirstOrDefault(a => a.Text == node.Text);
                if (o?.IsExpanded ?? false)
                {
                    node.IsExpanded = true;
                    SyncNodes(o.Nodes, node.Nodes);
                }
            }
        }


        static Dictionary<string, (DateTime lastWrite, byte[] checksum)> checksumCache
            = new Dictionary<string, (DateTime lastWrite, byte[] checksum)>();

        private static byte[] GetFileChecksum(FileInfo file)
        {
            if (checksumCache.TryGetValue(file.FullName, out var val))
            {
                if (file.LastWriteTimeUtc == val.lastWrite)
                    return val.checksum;
            }
            using (var fs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var checksum = SHA1.Create().ComputeHash(fs);
                checksumCache[file.FullName] = (file.LastWriteTimeUtc, checksum);
                return checksum;
            }
        }

        bool AreFilesSame(string folder1, string folder2, string filePath)
        {
            var file1 = new FileInfo(Path.Combine(folder1, filePath));
            var file2 = new FileInfo(Path.Combine(folder2, filePath));
            if (!file1.Exists)
                return true;//This means its a directory :)
            return GetFileChecksum(file1).SequenceEqual(GetFileChecksum(file2));
        }
    }
}