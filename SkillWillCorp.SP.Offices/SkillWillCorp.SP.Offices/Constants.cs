namespace SkillWillCorp.SP.Offices
{
    public class Constants
    {
        public class Fields
        {
            public const string NameFieldInternalName = "swc_Name";
            public const string DirectorFieldInternalName = "swc_DirectorUser";
            public const string MembersFieldInternalName = "swc_OfficeMembers";
        }

        public class Lists
        {
            public const string OfficesListUrl = "/lists/Offices";
            public const string Offices2ListUrl = "/lists/Offices2";
        }

        public class SecurityGroups
        {
            public const string OfficeOwners = "Office Owners";
            public const string OfficeMembers = "Office Members";
            public const string OfficeVisitors = "Office Visitors";
        }
    }
}
