using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;

namespace SkillWillCorp.SP.Offices
{
    public static class SPUtility
    {
        /// <summary>
        /// Gets Sharepoint list.
        /// </summary>
        /// <param name="web">from which to obtain a list of Web-site</param>
        /// <param name="webRelativeListUrl">Url-list, which must be obtained. Url-list regarding web-site transmitted</param>
        /// <returns>Sharepoint List</returns>
        public static SPList GetListUrl(this SPWeb web, string webRelativeListUrl)
        {
            if (web == null || string.IsNullOrEmpty(webRelativeListUrl))
            {
                return null;
            }

            try
            {
                return web.GetList(Microsoft.SharePoint.Utilities.SPUtility.ConcatUrls(web.ServerRelativeUrl, webRelativeListUrl));
            }
            catch (Exception ex)
            {
                Logger.WriteMessage(
                    string.Format("An error occurred while getting the list: {0} list address: {1} ", webRelativeListUrl,
                        Microsoft.SharePoint.Utilities.SPUtility.ConcatUrls(web.ServerRelativeUrl, webRelativeListUrl)), ex);
            }
            return null;
        }

        public static void CopyItemTo(this SPListItem srcItem, SPListItem destItem, List<string> includedFieldsStaticName = null)
        {
            foreach (SPField srcField in srcItem.Fields.Cast<SPField>().Where(field => !field.ReadOnlyField && field.InternalName != "Attachments"))
            {
                if (includedFieldsStaticName == null || !includedFieldsStaticName.Any())
                {
                    destItem[srcField.InternalName] = srcItem[srcField.InternalName];
                    continue;
                }

                var isProcessedField = includedFieldsStaticName.Any(f => f == srcField.StaticName);
                if (!isProcessedField)
                {
                    continue;
                }

                SPField destField = destItem.Fields.TryGetFieldByStaticName(srcField.StaticName);
                if (destField == null)
                {
                    continue;
                }

                try
                {
                    destItem[destField.Id] = srcItem[srcField.Id];
                }
                catch (Exception ex)
                {
                    Logger.WriteError(
                        string.Format(
                            "Cannot copy value from '{0}' field to '{1}' field with source value = '{2}'.",
                            srcField.Title, destField.Title, destItem[srcField.InternalName]), ex, TraceSeverity.High);
                    throw;
                }
            }

            destItem.Update();
        }

        public static SPFieldUserValue TryGetFieldUserValue(this SPListItem spListItem, dynamic fieldIdentifier)
        {
            SPFieldUserValue defaultValue = null;
            SPField field = null;

            try
            {
                if (fieldIdentifier is Guid)
                {
                    if (!spListItem.Fields.Contains((Guid)fieldIdentifier))
                    {
                        return defaultValue;
                    }
                    field = spListItem.Fields[(Guid)fieldIdentifier];
                }
                else if (fieldIdentifier is int)
                {
                    if (spListItem.Fields[(int)fieldIdentifier] == null)
                    {
                        return defaultValue;
                    }
                    field = spListItem.Fields[(int)fieldIdentifier];
                }
                else if (fieldIdentifier is string)
                {
                    if (!spListItem.Fields.ContainsField(fieldIdentifier as string))
                    {
                        if (!spListItem.Fields.ContainsFieldWithStaticName(fieldIdentifier as string))
                        {
                            return defaultValue;
                        }
                        field = spListItem.Fields.TryGetFieldByStaticName(fieldIdentifier as string);
                    }
                    else
                    {
                        field = spListItem.Fields.GetField(fieldIdentifier as string);
                    }
                }

                if (field == null)
                {
                    return defaultValue;
                }

                defaultValue = new SPFieldUserValue(spListItem.Web, spListItem[field.Id].ToString());
                return defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        public static SPFieldUserValueCollection TryGetFieldUserValueCollection(this SPListItem spListItem, dynamic fieldIdentifier)
        {
            SPFieldUserValueCollection defaultValue = null;
            SPField field = null;

            try
            {
                if (fieldIdentifier is Guid)
                {
                    if (!spListItem.Fields.Contains((Guid)fieldIdentifier))
                    {
                        return defaultValue;
                    }
                    field = spListItem.Fields[(Guid)fieldIdentifier];
                }
                else if (fieldIdentifier is int)
                {
                    if (spListItem.Fields[(int)fieldIdentifier] == null)
                    {
                        return defaultValue;
                    }
                    field = spListItem.Fields[(int)fieldIdentifier];
                }
                else if (fieldIdentifier is string)
                {
                    if (!spListItem.Fields.ContainsField(fieldIdentifier as string))
                    {
                        if (!spListItem.Fields.ContainsFieldWithStaticName(fieldIdentifier as string))
                        {
                            return defaultValue;
                        }
                        field = spListItem.Fields.TryGetFieldByStaticName(fieldIdentifier as string);
                    }
                    else
                    {
                        field = spListItem.Fields.GetField(fieldIdentifier as string);
                    }
                }

                if (field == null)
                {
                    return defaultValue;
                }

                defaultValue = new SPFieldUserValueCollection(spListItem.Web, spListItem[field.Id].ToString());
                return defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// We receive safe field value
        /// </summary>
        /// <typeparam name="T">Type of return value</typeparam>
        /// <param name="spListItem">The SPListItem to get field value</param>
        /// <param name="fieldIdentifier">Guid - Id field, string - the name of the field (display or internal or static), int - the index of the field</param>
        /// <param name="defaultValue">Will returned if something is wrong</param>
        /// <returns>Value of search field in listitem</returns>
        public static T TryGetFieldValue<T>(this SPListItem spListItem, dynamic fieldIdentifier, T defaultValue)
        {
            SPField field = null;
            try
            {
                if (fieldIdentifier is Guid)
                {
                    if (!spListItem.Fields.Contains((Guid)fieldIdentifier))
                    {
                        return defaultValue;
                    }
                    field = spListItem.Fields[(Guid)fieldIdentifier];
                }
                else if (fieldIdentifier is int)
                {
                    if (spListItem.Fields[(int)fieldIdentifier] == null)
                    {
                        return defaultValue;
                    }
                    field = spListItem.Fields[(int)fieldIdentifier];
                }
                else if (fieldIdentifier is string)
                {
                    if (!spListItem.Fields.ContainsField(fieldIdentifier as string))
                    {
                        if (!spListItem.Fields.ContainsFieldWithStaticName(fieldIdentifier as string))
                        {
                            return defaultValue;
                        }
                        field = spListItem.Fields.TryGetFieldByStaticName(fieldIdentifier as string);
                    }
                    else
                    {
                        field = spListItem.Fields.GetField(fieldIdentifier as string);
                    }
                }

                if (field == null)
                {
                    return defaultValue;
                }

                var value = (T)spListItem[field.Id];
                return value;
            }
            catch
            {
                return defaultValue;
            }
        }

        public static string TryGetFieldValueAsText(this SPListItem spListItem, dynamic fieldIdentifier)
        {
            var defaultValue = string.Empty;
            SPField field = null;
            try
            {
                if (fieldIdentifier is Guid)
                {
                    if (!spListItem.Fields.Contains((Guid)fieldIdentifier))
                    {
                        return defaultValue;
                    }
                    field = spListItem.Fields[(Guid)fieldIdentifier];
                }
                else if (fieldIdentifier is int)
                {
                    if (spListItem.Fields[(int)fieldIdentifier] == null)
                    {
                        return defaultValue;
                    }
                    field = spListItem.Fields[(int)fieldIdentifier];
                }
                else if (fieldIdentifier is string)
                {
                    if (!spListItem.Fields.ContainsField(fieldIdentifier as string))
                    {
                        if (!spListItem.Fields.ContainsFieldWithStaticName(fieldIdentifier as string))
                        {
                            return defaultValue;
                        }
                        field = spListItem.Fields.TryGetFieldByStaticName(fieldIdentifier as string);
                    }
                    else
                    {
                        field = spListItem.Fields.GetField(fieldIdentifier as string);
                    }
                }

                if (field == null)
                {
                    return defaultValue;
                }

                var value = defaultValue;

                if (fieldIdentifier is Guid)
                {
                    value = field.GetFieldValueAsText(spListItem[(Guid)fieldIdentifier]);
                }
                else if (fieldIdentifier is string)
                {
                    value = field.GetFieldValueAsText(spListItem[fieldIdentifier as string]);
                }
                else if (fieldIdentifier is int)
                {
                    value = field.GetFieldValueAsText(spListItem[(int)fieldIdentifier]);
                }

                return value;
            }
            catch
            {
                return defaultValue;
            }
        }
    }
}
