using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Mount;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WatchTemplate.Services
{
    public class WorkaroundTemplateInfo : ITemplate
    {
        public WorkaroundTemplateInfo(RunnableProjectTemplate source)
        {
            Author = source.Author;
            Description = source.Description;
            Classifications = source.Classifications;
            DefaultName = source.DefaultName;
            Identity = source.Identity;
            GeneratorId = source.GeneratorId;
            GroupIdentity = source.GroupIdentity;
            Precedence = source.Precedence;
            Name = source.Name;
            ShortName = source.ShortName;
            Tags = source.Tags;
            CacheParameters = source.CacheParameters;
            Parameters = source.Parameters;
            if (source.ConfigFile != null)
            {
                ConfigMountPointId = source.ConfigMountPointId;
                ConfigPlace = source.ConfigPlace;
            }
            if (source.LocaleConfigFile != null)
            {
                LocaleConfigMountPointId = source.LocaleConfigMountPointId;
                LocaleConfigPlace = source.LocaleConfigPlace;
            }
            HostConfigMountPointId = source.HostConfigMountPointId;
            HostConfigPlace = source.HostConfigPlace;
            ThirdPartyNotices = source.ThirdPartyNotices;
            BaselineInfo = source.BaselineInfo;
            HasScriptRunningPostActions = source.HasScriptRunningPostActions;

            if (source is IShortNameList sourceWithShortNameList)
            {
                ShortNameList = sourceWithShortNameList.ShortNameList;
            }

            if (source is ITemplateWithTimestamp withTimestamp)
            {
                ConfigTimestampUtc = withTimestamp.ConfigTimestampUtc;
            }

            Generator = source.Generator;
            Configuration = source.Configuration;
            LocaleConfiguration = source.LocaleConfiguration;
            TemplateSourceRoot = source.TemplateSourceRoot;
            IsNameAgreementWithFolderPreferred = source.IsNameAgreementWithFolderPreferred;
        }

        public string Author { get; private set; }

        public string Description { get; private set; }

        public IReadOnlyList<string> Classifications { get; private set; }

        public string DefaultName { get; private set; }

        public string Identity { get; private set; }

        public Guid GeneratorId { get; private set; }

        public string GroupIdentity { get; private set; }

        public int Precedence { get; private set; }

        public string Name { get; private set; }

        public string ShortName { get; private set; }

        public IReadOnlyList<string> ShortNameList { get; set; }

        public IReadOnlyList<string> GroupShortNameList { get; set; }

        public IReadOnlyDictionary<string, ICacheTag> Tags { get; private set; }

        public IReadOnlyDictionary<string, ICacheParameter> CacheParameters { get; private set; }

        public IReadOnlyList<ITemplateParameter> Parameters { get; private set; }

        public Guid ConfigMountPointId { get; private set; }

        public string ConfigPlace { get; private set; }

        public Guid LocaleConfigMountPointId { get; private set; }

        public string LocaleConfigPlace { get; private set; }

        public Guid HostConfigMountPointId { get; private set; }

        public string HostConfigPlace { get; private set; }

        public string ThirdPartyNotices { get; private set; }

        public IReadOnlyDictionary<string, IBaselineInfo> BaselineInfo { get; private set; }

        public bool HasScriptRunningPostActions { get; set; }

        public DateTime? ConfigTimestampUtc { get; set; }

        public IGenerator Generator { get; private set; }

        public IFileSystemInfo Configuration { get; private set; }

        public IFileSystemInfo LocaleConfiguration { get; private set; }

        public IDirectory TemplateSourceRoot { get; private set; }

        public bool IsNameAgreementWithFolderPreferred { get; private set; }
    }
}
