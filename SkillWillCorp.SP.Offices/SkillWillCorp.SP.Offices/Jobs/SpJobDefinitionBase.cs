using System;
using System.Linq;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;

namespace SkillWillCorp.SP.Offices.Jobs
{
    public abstract class SpJobDefinitionBase : SPJobDefinition
    {
        protected string WebUrl;
        protected string SiteUrl;

        private const string WebUrlParamKey = "WebUrl";
        private const string SiteUrlParamKey = "SiteUrl";

        private readonly string _jobName;

        protected SpJobDefinitionBase() : base() { }

        protected SpJobDefinitionBase(SPWebApplication webApplication, SPWeb web, string jobName, string jobTitle)
            : base(jobName, webApplication, SPServer.Local, SPJobLockType.Job)
        {
            _jobName = jobName;
            Title = jobTitle;
            SaveProperties(web);
        }

        public void DeleteIfExistJob()
        {
            SPJobDefinition existsJob = WebApplication.JobDefinitions.SingleOrDefault(x => x.Name.Equals(_jobName));
            if (existsJob != null)
            {
                existsJob.Delete();
            }
        }

        private void InitProperties()
        {
            SiteUrl = Convert.ToString(Properties[SiteUrlParamKey]);
            WebUrl = Convert.ToString(Properties[WebUrlParamKey]);
        }

        private void SaveProperties(SPWeb web)
        {
            Properties[WebUrlParamKey] = web.ServerRelativeUrl;
            Properties[SiteUrlParamKey] = web.Site.Url;
        }

        private bool IsValid()
        {
            if (string.IsNullOrEmpty(SiteUrl))
            {
                Logger.WriteMessage(String.Format("Timer job {0} It can not be started, because the object is empty siteUrl", Title));
                return false;
            }
            if (string.IsNullOrEmpty(WebUrl))
            {
                Logger.WriteMessage(String.Format("Timer job {0} It can not be started, because the object is empty webUrl", Title));
                return false;
            }

            return true;
        }

        /// <summary>
        /// Use the method ProcessJob, this method is called when Joba passed validation and field WebUrl and SiteUrl, have been successfully initialized
        /// </summary>
        public override void Execute(Guid targetInstanceId)
        {
            InitProperties();
            if (!IsValid())
            {
                return;
            }

            ProcessJob(targetInstanceId);
        }

        protected abstract void ProcessJob(Guid targetInstanceId);

        protected override bool HasAdditionalUpdateAccess()
        {
            return true;
        }
    }
}
