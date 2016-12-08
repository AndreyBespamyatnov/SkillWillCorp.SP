using System;
using System.Linq;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;

namespace SkillWillCorp.SP.Offices.Provisioning.Jobs
{
    public class ListSynchronizationJob : SpJobDefinitionBase
    {
        private const string JobName = "ListSynchronizationJob";
        private const string JobTitle = "Copy list items from Offices list to Offices2 list";

        /// <summary> 
        /// Пустой конструктор нужен для шарика
        /// </summary>
        public ListSynchronizationJob() { }
        public ListSynchronizationJob(SPWebApplication webApplication, SPWeb web) : base(webApplication, web, JobName, JobTitle) { }

        protected override void ProcessJob(Guid targetInstanceId)
        {
            // проверяет есть ли запущенные экземпляры данного timerjob'а
            // если есть - работа прекращается
            if ((from SPRunningJob job in WebApplication.RunningJobs
                 where String.CompareOrdinal(job.JobDefinition.Name, Name) == 0
                 select job).Count() > 1)
            {
                Logger.WriteMessage(string.Format("Невозможно запустить несколько экземпляров одного timer job. Имя: {0}, Время попытки запуска: {1}", Title,
                    DateTime.Now.ToString("dd:MM:yyyy:hh:mm:ss")));
                return;
            }

            using (SPSite site = new SPSite(SiteUrl, SPUserToken.SystemAccount))
            {
                using (SPWeb web = site.OpenWeb(WebUrl))
                {
                    try
                    {
                        SPList officesList = web.GetList(Constants.Lists.OfficesListUrl);
                        SPList officesList2 = web.GetList(Constants.Lists.Offices2ListUrl);

                        // TODO

                    }
                    catch (Exception ex)
                    {
                        Logger.WriteError(
                            string.Format("Не удалось скопировать элементы списка {0} в список {1}.", Constants.Lists.OfficesListUrl, Constants.Lists.Offices2ListUrl),
                            ex, 
                            TraceSeverity.Unexpected);
                    }
                }
            }
        }
    }
}
