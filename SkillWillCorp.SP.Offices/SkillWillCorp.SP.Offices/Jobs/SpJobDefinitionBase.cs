using System;
using System.Linq;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;

namespace SkillWillCorp.SP.Offices.Provisioning.Jobs
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
                Logger.WriteMessage(String.Format("Timer job {0} не может быть запущен, так как объект siteUrl пуст", Title));
                return false;
            }
            if (string.IsNullOrEmpty(WebUrl))
            {
                Logger.WriteMessage(String.Format("Timer job {0} не может быть запущен, так как объект webUrl пуст", Title));
                return false;
            }

            return true;
        }

        /// <summary>
        /// Используйте метод ProcessJob, этот метод вызывается если джоба прошла валидацию
        /// и поля WebUrl и SiteUrl, были удачно инициализированны
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
    }
}
