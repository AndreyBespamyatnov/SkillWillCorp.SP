using System;
using System.Linq;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;
using NecroNet.SharePoint.CodeCaml;
using SPMeta2.Definitions;
using SPMeta2.Enumerations;
using SPMeta2.SSOM.Services;
using SPMeta2.Syntax.Default;

namespace SkillWillCorp.SP.Offices.EventReceiver
{
    public class OfficesEventReceiver : SPItemEventReceiver
    {
        public override void ItemAdded(SPItemEventProperties properties)
        {
            base.ItemAdded(properties);

            try
            {
                var name = GetNameFieldValue(properties);
                var director = GetDirectorFieldValue(properties);
                var members = GetMembersFieldValue(properties);
                if (IsNotValidate(name, director, members)) return;

                CreateOrUpdateSubSite(properties.Site, name, properties.ListItemId, director, members);
            }
            catch (Exception ex)
            {
                Logger.WriteError(ex.ToString(), ex, TraceSeverity.Unexpected);
            }
        }

        public override void ItemDeleted(SPItemEventProperties properties)
        {
            base.ItemDeleted(properties);

            try
            {
                DeleteSubSite(properties.Site, properties.ListItemId);
            }
            catch (Exception ex)
            {
                Logger.WriteError(ex.ToString(), ex, TraceSeverity.Unexpected);
            }
        }

        public override void ItemUpdated(SPItemEventProperties properties)
        {
            try
            {
                var name = GetNameFieldValue(properties);
                var director = GetDirectorFieldValue(properties);
                var members = GetMembersFieldValue(properties);
                if (IsNotValidate(name, director, members)) return;

                CreateOrUpdateSubSite(properties.Site, name, properties.ListItemId, director, members);
            }
            catch (Exception ex)
            {
                Logger.WriteError(ex.ToString(), ex, TraceSeverity.Unexpected);
            }
        }

        private static string GetNameFieldValue(SPItemEventProperties properties)
        {
            SPList list = properties.Web.GetListUrl(Constants.Lists.OfficesListUrl);
            SPField field = list.Fields.TryGetFieldByStaticName(Constants.Fields.NameFieldInternalName);
            var name = properties.ListItem[field.Id] as string;
            return name;
        }

        private static SPFieldUserValue GetDirectorFieldValue(SPItemEventProperties properties)
        {
            SPList list = properties.Web.GetListUrl(Constants.Lists.OfficesListUrl);
            var field = list.Fields.TryGetFieldByStaticName(Constants.Fields.DirectorFieldInternalName) as SPFieldUser;
            if (field == null || properties.ListItem[field.Id] == null)
            {
                return null;
            }

            var directorUser = new SPFieldUserValue(properties.Site.RootWeb, properties.ListItem[field.Id].ToString());
            return directorUser;
        }
        private static SPFieldUserValueCollection GetMembersFieldValue(SPItemEventProperties properties)
        {
            SPList list = properties.Web.GetListUrl(Constants.Lists.OfficesListUrl);
            var field = list.Fields.TryGetFieldByStaticName(Constants.Fields.MembersFieldInternalName) as SPFieldUser;
            if (field == null || properties.ListItem[field.Id] == null)
            {
                return null;
            }

            var directorUser = new SPFieldUserValueCollection(properties.Site.RootWeb, properties.ListItem[field.Id].ToString());
            return directorUser;
        }

        private bool IsNotValidate(string name, SPFieldUserValue director, SPFieldUserValueCollection members)
        {
            bool hasNotName = string.IsNullOrWhiteSpace(name);
            bool hasNotDirector = director == null;
            bool hasNotMembers = members == null || !members.Any();

            return hasNotName || hasNotDirector || hasNotMembers;
        }

        private static void CreateOrUpdateSubSite(SPSite spSite, string siteName, int itemId, SPFieldUserValue director,  SPFieldUserValueCollection members)
        {
            string siteUrl = "user-web-" + itemId;
            string securityGroupNameFormat = "{0} - {1}";

            var breakRoleInheritanceDefinition = new BreakRoleInheritanceDefinition
            {
                CopyRoleAssignments = false
            };

            var ownersGroup = new SecurityGroupDefinition
            {
                Name = string.Format(securityGroupNameFormat, siteName, Constants.SecurityGroups.OfficeOwners),
                IsAssociatedOwnerGroup = true,
                Owner = director.LoginName
            };
            var membersGroup = new SecurityGroupDefinition
            {
                Name = string.Format(securityGroupNameFormat, siteName, Constants.SecurityGroups.OfficeMembers),
                IsAssociatedMemberGroup = true,
                Owner = director.LoginName
            };
            var visitorsGroup = new SecurityGroupDefinition
            {
                Name = string.Format(securityGroupNameFormat, siteName, Constants.SecurityGroups.OfficeVisitors),
                IsAssociatedVisitorsGroup = true,
                Owner = director.LoginName
            };

            // site model with the groups
            var siteModel = SPMeta2Model.NewSiteModel(site =>
            {
                site.AddSecurityGroup(ownersGroup);
                site.AddSecurityGroup(membersGroup);
                site.AddSecurityGroup(visitorsGroup);
            });

            var webDefinition = new WebDefinition
            {
                Title = siteName,
                Description = "",
                Url = siteUrl,
                WebTemplate = BuiltInWebTemplates.Collaboration.BlankSite,
            };
            // web model
            var webModel = SPMeta2Model.NewWebModel(web =>
            {
                web.AddWeb(webDefinition, addedWeb =>
                {
                    addedWeb.AddBreakRoleInheritance(breakRoleInheritanceDefinition, resetWeb =>
                    {
                        // add group with owner permission
                        resetWeb.AddSecurityGroupLink(ownersGroup, group =>
                        {
                            @group.AddSecurityRoleLink(new SecurityRoleLinkDefinition
                            {
                                SecurityRoleType = BuiltInSecurityRoleTypes.Administrator
                            });
                        });
                        // add group with contributor permission
                        resetWeb.AddSecurityGroupLink(membersGroup, group =>
                        {
                            @group.AddSecurityRoleLink(new SecurityRoleLinkDefinition
                            {
                                SecurityRoleType = BuiltInSecurityRoleTypes.Contributor
                            });
                        });
                        // add group with reader permission
                        resetWeb.AddSecurityGroupLink(visitorsGroup, group =>
                        {
                            @group.AddSecurityRoleLink(new SecurityRoleLinkDefinition
                            {
                                SecurityRoleType = BuiltInSecurityRoleTypes.Reader
                            });
                        });
                    });
                });
            });
            var csomProvisionService = new SSOMProvisionService();
            csomProvisionService.DeploySiteModel(spSite, siteModel);
            csomProvisionService.DeployWebModel(spSite.RootWeb, webModel);

            SPWeb existWeb = spSite.AllWebs.SingleOrDefault(w => w.Url.Contains(siteUrl));
            if (existWeb == null)
            {
                return;
            }

            SPGroup spMembersGroup =
                existWeb.SiteGroups.Cast<SPGroup>()
                    .FirstOrDefault(
                        siteGroup =>
                            siteGroup.Name ==
                            string.Format(securityGroupNameFormat, siteName, Constants.SecurityGroups.OfficeMembers));
            if (spMembersGroup == null)
            {
                return;
            }

            foreach (SPFieldUserValue member in members)
            {
                spMembersGroup.AddUser(member.User);
            }
        }

        private static void DeleteSubSite(SPSite spSite, int itemId)
        {
            string siteUrl = "user-web-" + itemId;
            SPWeb existWeb = spSite.AllWebs.SingleOrDefault(w => w.Url.Contains(siteUrl));
            if (existWeb == null) return;
            existWeb.Delete();
            existWeb.Update();
        }
    }
}
