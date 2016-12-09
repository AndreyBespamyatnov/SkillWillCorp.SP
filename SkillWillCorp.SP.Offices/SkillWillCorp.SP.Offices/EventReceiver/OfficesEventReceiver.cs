using System;
using System.Collections.Generic;
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

            var newWebDef = new WebDefinition
            {
                Title = siteName,
                Description = "",
                Url = siteUrl,
                WebTemplate = BuiltInWebTemplates.Collaboration.TeamSite
            };

            var newWebBreakRoleInheritance = new BreakRoleInheritanceDefinition
            {
                CopyRoleAssignments = false
            };

            var ownersGroup = new SecurityGroupDefinition
            {
                Name = string.Format(securityGroupNameFormat, siteName, Constants.SecurityGroups.OfficeOwners),
                Owner = director.LoginName
            };
            var membersGroup = new SecurityGroupDefinition
            {
                Name = string.Format(securityGroupNameFormat, siteName, Constants.SecurityGroups.OfficeMembers),
                Owner = director.LoginName
            };
            var visitorsGroup = new SecurityGroupDefinition
            {
                Name = string.Format(securityGroupNameFormat, siteName, Constants.SecurityGroups.OfficeVisitors),
                Owner = director.LoginName
            };
 
            // site model with the groups
            var siteModel = SPMeta2Model.NewSiteModel(site =>
            {
                site.AddSecurityGroup(ownersGroup);
                site.AddSecurityGroup(membersGroup);
                site.AddSecurityGroup(visitorsGroup);
            });
 
            // web model
            var webModel = SPMeta2Model.NewWebModel(web =>
            {
                web.AddWeb(newWebDef, publicProjectWeb =>
                {
                    publicProjectWeb.AddBreakRoleInheritance(newWebBreakRoleInheritance, newResetWeb =>
                    {
                        // add group with owner permission
                        newResetWeb.AddSecurityGroupLink(ownersGroup, group =>
                        {
                            group.AddSecurityRoleLink(new SecurityRoleLinkDefinition
                            {
                                SecurityRoleType = BuiltInSecurityRoleTypes.Administrator
                            });
                        });
                        // add group with contributor permission
                        newResetWeb.AddSecurityGroupLink(membersGroup, group =>
                        {
                            group.AddSecurityRoleLink(new SecurityRoleLinkDefinition
                            {
                                SecurityRoleType = BuiltInSecurityRoleTypes.Contributor
                            });
                        });
 
                        // add group with reader permission
                        newResetWeb.AddSecurityGroupLink(visitorsGroup, group =>
                        {
                            group.AddSecurityRoleLink(new SecurityRoleLinkDefinition
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

            // add users to members group
            SPGroup spOwnerGroup = existWeb.SiteGroups.Cast<SPGroup>().FirstOrDefault(siteGroup => siteGroup.Name == string.Format(securityGroupNameFormat, siteName, Constants.SecurityGroups.OfficeOwners));
            if (spOwnerGroup != null)
            {
                spOwnerGroup.AddUser(director.User);
            }
            SPGroup spMembersGroup = existWeb.SiteGroups.Cast<SPGroup>().FirstOrDefault(siteGroup => siteGroup.Name == string.Format(securityGroupNameFormat, siteName, Constants.SecurityGroups.OfficeMembers));
            if (spMembersGroup != null)
            {
                foreach (SPFieldUserValue member in members)
                {
                    spMembersGroup.AddUser(member.User);
                }
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
