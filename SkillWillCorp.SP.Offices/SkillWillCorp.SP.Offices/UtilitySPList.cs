using System;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Utilities;

namespace SkillWillCorp.SP.Offices
{
    public static class UtilitySPList
    {
        /// <summary>
        /// Получает лист Sharepoint.
        /// </summary>
        /// <param name="web">Web-узел из которого необходимо получить список</param>
        /// <param name="webRelativeListUrl">Url-списка, который необходимо получить. Url-списка относительно переданного web-узла</param>
        /// <returns>Список Sharepoint</returns>
        public static SPList GetListUrl(this SPWeb web, string webRelativeListUrl)
        {
            if (web == null || string.IsNullOrEmpty(webRelativeListUrl))
            {
                return null;
            }

            try
            {
                return web.GetList(SPUtility.ConcatUrls(web.ServerRelativeUrl, webRelativeListUrl));
            }
            catch (Exception ex)
            {
                Logger.WriteMessage(
                    string.Format("Произошла ошибка при получении списка: {0} адрес списка: {1} ", webRelativeListUrl,
                        SPUtility.ConcatUrls(web.ServerRelativeUrl, webRelativeListUrl)), ex);
            }
            return null;
        }
    }
}
