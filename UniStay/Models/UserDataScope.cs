

namespace UniStay.Models
{
    public class UserDataScope
    {
        public int SystemUserID { get; set; }
        public int DataScopeID { get; set; }

        public SystemUser SystemUser { get; set; } = null!;
        public DataScope DataScope { get; set; } = null!;
    }
}