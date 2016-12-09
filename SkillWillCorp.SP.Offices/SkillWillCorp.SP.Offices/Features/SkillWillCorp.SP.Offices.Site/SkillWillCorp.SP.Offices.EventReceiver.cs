using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.SharePoint;
using SkillWillCorp.SP.Offices.Jobs;
using SPMeta2.Definitions;
using SPMeta2.Enumerations;
using SPMeta2.SSOM.Services;
using SPMeta2.Syntax.Default;

namespace SkillWillCorp.SP.Offices.Features.SkillWillCorp.SP.Offices.Site
{
    /// <summary>
    /// This class handles events raised during feature activation, deactivation, installation, uninstallation, and upgrade.
    /// </summary>
    /// <remarks>
    /// The GUID attached to this class may be used during packaging and should not be modified.
    /// </remarks>
    [Guid("c58a2eb4-e724-497d-96f3-8ec3cee39b80")]
    public class SkillWillCorpSPOfficesEventReceiver : SPFeatureReceiver
    {
        public override void FeatureActivated(SPFeatureReceiverProperties properties)
        {
            const string assembly = "SkillWillCorp.SP.Offices, Version=1.0.0.0, Culture=neutral, PublicKeyToken=f54f8a30b400bb4c";
            const string eventReceiverClasss = "SkillWillCorp.SP.Offices.EventReceiver.OfficesEventReceiver";

            SPSite site = properties.Feature.Parent as SPSite;
            if (site == null)
            {
                throw new Exception("Critical error: SPSite is not found.");
            }

            var nameField = new FieldDefinition
            {
                Title = "Name",
                InternalName = Constants.Fields.NameFieldInternalName,
                Group = "SWC.Offices",
                Id = new Guid(Constants.Fields.NameFieldInternalNameId),
                AddToDefaultView = true,
                FieldType = BuiltInFieldTypes.Text,
                Required = true
            };

            var directorField = new FieldDefinition
            {
                Title = "Director (User)",
                InternalName = Constants.Fields.DirectorFieldInternalName,
                Group = "SWC.Offices",
                Id = new Guid(Constants.Fields.DirectorFieldInternalNameId),
                AddToDefaultView = true,
                FieldType = BuiltInFieldTypes.User,
                Required = true
            };

            var descriptionField = new FieldDefinition
            {
                Title = "Description",
                InternalName = "swc_Description",
                Group = "SWC.Offices",
                Id = new Guid("ADD8A4AF-0BDD-438E-886F-7767368B56FB"),
                AddToDefaultView = true,
                FieldType = BuiltInFieldTypes.Note
            };

            var officeCodeField = new FieldDefinition
            {
                Title = "Office Code",
                InternalName = "swc_OfficeCode",
                Group = "SWC.Offices",
                Id = new Guid("438893A6-3D5C-4B84-86B8-C0C8D8F1183B"),
                AddToDefaultView = true,
                FieldType = BuiltInFieldTypes.Text
            };

            var officeMembersField = new FieldDefinition
            {
                Title = "Office Members (Users)",
                InternalName = Constants.Fields.MembersFieldInternalName,
                Group = "SWC.Offices",
                Id = new Guid(Constants.Fields.MembersFieldInternalNameId),
                AddToDefaultView = true,
                FieldType = BuiltInFieldTypes.UserMulti,
                Required = true,
                AdditionalAttributes =
                    new List<FieldAttributeValue>(new List<FieldAttributeValue>
                    {
                        new FieldAttributeValue("Mult", "TRUE")
                    })
            };

            var fieldIsCopiedField = new FieldDefinition
            {
                Title = "Field is copied",
                InternalName = Constants.Fields.FieldIsCopiedFieldInternalName,
                Group = "SWC.Offices",
                AddToDefaultView = false,
                Hidden = true,
                Id = new Guid(Constants.Fields.FieldIsCopiedFieldInternalNameId),
                FieldType = BuiltInFieldTypes.Boolean
            };

            var officesList = new ListDefinition
            {
                Title = "Offices",
                Description = "",
                TemplateType = BuiltInListTemplateTypeId.GenericList,
                Url = "Offices",
                OnQuickLaunch = true
            };

            var officesList2 = new ListDefinition
            {
                Title = "Offices2",
                Description = "",
                TemplateType = BuiltInListTemplateTypeId.GenericList,
                Url = "Offices2",
                OnQuickLaunch = true
            };

            var officesEventReceiverItemAdded = new EventReceiverDefinition
            {
                Assembly = assembly,
                Class = eventReceiverClasss,
                Synchronization = BuiltInEventReceiverSynchronization.Synchronous,
                Type = BuiltInEventReceiverType.ItemAdded,
                Name = "officesEventReceiverItemAdded",
                SequenceNumber = 10000
            };

            var officesEventReceiverItemDeleted = new EventReceiverDefinition
            {
                Assembly = assembly,
                Class = eventReceiverClasss,
                Synchronization = BuiltInEventReceiverSynchronization.Synchronous,
                Type = BuiltInEventReceiverType.ItemDeleted,
                Name = "officesEventReceiverItemDeleted",
                SequenceNumber = 10000
            };

            var officesEventReceiverItemUpdated = new EventReceiverDefinition
            {
                Assembly = assembly,
                Class = eventReceiverClasss,
                Synchronization = BuiltInEventReceiverSynchronization.Synchronous,
                Type = BuiltInEventReceiverType.ItemUpdated,
                Name = "officesEventReceiverItemUpdated",
                SequenceNumber = 10000
            };

            var model = SPMeta2Model.NewWebModel(web =>
            {
                web.AddFields(new List<FieldDefinition>
                {
                    nameField,
                    directorField,
                    descriptionField,
                    officeCodeField,
                    officeMembersField,
                    fieldIsCopiedField
                });
                web.AddList(officesList, list =>
                {
                    list.AddField(nameField);
                    list.AddField(directorField);
                    list.AddField(descriptionField);
                    list.AddField(officeCodeField);
                    list.AddField(officeMembersField);
                    list.AddField(fieldIsCopiedField);

                    list.AddEventReceivers(new List<EventReceiverDefinition>
                    {
                        officesEventReceiverItemAdded,
                        officesEventReceiverItemUpdated,
                        officesEventReceiverItemDeleted
                    });
                });
                web.AddList(officesList2, list =>
                {
                    list.AddField(nameField);
                    list.AddField(directorField);
                });
            });

            SPWeb spWeb = site.RootWeb;
            var csomProvisionService = new SSOMProvisionService();
            csomProvisionService.DeployWebModel(spWeb, model);
            CreateDeleteJob(spWeb);

        }

        /// <summary> 
        /// Create / update Job to copy items
        /// </summary>
        private static void CreateDeleteJob(SPWeb web)
        {
            Logger.WriteMessage("SkillWillCorp.SP.Offices.SkillWillCorpSPOfficesEventReceiver: Create timers to copy elements");
            SPSecurity.RunWithElevatedPrivileges(delegate
            {
                var deleteJob = new ListSynchronizationJob(web.Site.WebApplication, web);
                deleteJob.DeleteIfExistJob();
                var schedule = new SPMinuteSchedule { Interval = 30 };
                deleteJob.Schedule = schedule;
                deleteJob.Update();
                deleteJob.RunNow();
            });
        }
    }
}
