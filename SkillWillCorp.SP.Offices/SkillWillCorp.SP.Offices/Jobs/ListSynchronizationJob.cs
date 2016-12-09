using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;
using NecroNet.SharePoint.CodeCaml;

namespace SkillWillCorp.SP.Offices.Jobs
{
    public class ListSynchronizationJob : SpJobDefinitionBase
    {
        private const string JobName = "ListSynchronizationJob";
        private const string JobTitle = "Copy list items from Offices list to Offices2 list";

        /// <summary> 
        /// An empty constructor is needed for the sharepoint jobs
        /// </summary>
        public ListSynchronizationJob() { }
        public ListSynchronizationJob(SPWebApplication webApplication, SPWeb web) : base(webApplication, web, JobName, JobTitle) { }

        protected override void ProcessJob(Guid targetInstanceId)
        {
            // check whether there are running instances of timerjob
            // if there is - the work stops
            if ((from SPRunningJob job in WebApplication.RunningJobs
                 where String.CompareOrdinal(job.JobDefinition.Name, Name) == 0
                 select job).Count() > 1)
            {
                Logger.WriteMessage(string.Format("You can not run multiple instances of the same timer job. Name: {0}, Time start attempt: {1}", Title,
                    DateTime.Now.ToString("dd:MM:yyyy:hh:mm:ss")));
                return;
            }

            using (var site = new SPSite(SiteUrl, SPUserToken.SystemAccount))
            {
                using (SPWeb web = site.OpenWeb(WebUrl))
                {
                    try
                    {
                        SPList officesList = web.GetList(Constants.Lists.OfficesListUrl);
                        SPList officesList2 = web.GetList(Constants.Lists.Offices2ListUrl);

                        // prepare spquery ti get not copied items by 'iscopied' flag
                        string query = CQ.Where(CQ.Neq.FieldRef(new Guid(Constants.Fields.FieldIsCopiedFieldInternalNameId)).Value(true));
                        var spQuery = new SPQuery
                        {
                            Query = query,
                            ViewFields =
                                CQ.ViewFields(CQ.FieldRef(new Guid(Constants.Fields.NameFieldInternalNameId)),
                                    CQ.FieldRef(new Guid(Constants.Fields.DirectorFieldInternalNameId)),
                                    CQ.FieldRef(new Guid(Constants.Fields.FieldIsCopiedFieldInternalNameId)),
                                    CQ.FieldRef(SPBuiltInFieldId.Title))
                        };

                        // get not copied items
                        SPListItemCollection itemsToCopy = officesList.GetItems(spQuery);
                        SPListItem destItem = officesList2.Items.Add();

                        foreach (SPListItem srcItem in itemsToCopy)
                        {
                            // set additional title field
                            destItem[SPBuiltInFieldId.Title] = srcItem[SPBuiltInFieldId.Title];

                            // Copy fields
                            srcItem.CopyItemTo(destItem,
                                new List<string>
                                {
                                    Constants.Fields.NameFieldInternalName,
                                    Constants.Fields.DirectorFieldInternalName,
                                });

                            // set flag copied to 'true' at source item
                            srcItem[new Guid(Constants.Fields.FieldIsCopiedFieldInternalNameId)] = true;
                            srcItem.Update();
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteError(
                            string.Format("Failed to copy the elements of the list {0} to the list {1}.", Constants.Lists.OfficesListUrl, Constants.Lists.Offices2ListUrl),
                            ex, 
                            TraceSeverity.Unexpected);
                    }
                }
            }
        }
    }
}
