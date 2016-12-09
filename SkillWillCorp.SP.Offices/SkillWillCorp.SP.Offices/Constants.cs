namespace SkillWillCorp.SP.Offices
{
    public class Constants
    {
        public class Fields
        {
            public const string NameFieldInternalName = "swc_Name";
            public const string NameFieldInternalNameId = "BA13C7FB-63C2-49FB-92BA-DF0518A1865C";

            public const string DirectorFieldInternalName = "swc_DirectorUser";
            public const string DirectorFieldInternalNameId = "42F048A2-DC3B-417D-BE3D-77549CA2EC83";

            public const string MembersFieldInternalName = "swc_OfficeMembers";
            public const string MembersFieldInternalNameId = "C4EAE3A6-A2DB-422C-A748-0D278FB04FE0";

            public const string FieldIsCopiedFieldInternalName = "swc_FieldIsCopied";
            public const string FieldIsCopiedFieldInternalNameId = "BD71C005-F09B-48DA-9921-C76AC01A20E0";
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
