using System;
using System.Collections.Generic;
namespace UniStay.Models
{
    public class DataScope
    {
        public int ID { get; set; }
        public string ScopeType { get; set; } = string.Empty;     // All, MaleOnly, FemaleOnly, DormitoryCity, Building, Faculty
        public string? ScopeValue { get; set; }                    // "CityID:5" أو "BuildingID:3" أو "Medicine"

            public virtual ICollection<SystemUser> SystemUsers { get; set; } = new List<SystemUser>();

        public ICollection<UserDataScope> UserDataScopes { get; set; } = new List<UserDataScope>();
    }
}   
