using System;
using System.Collections.Generic;

namespace UniStay.Models;

public partial class HousingInstruction
{
    public int ID { get; set; }

    public int? DormitoryCityID { get; set; }

    public string Title { get; set; } = null!;

    public string Content { get; set; } = null!;

    public string InstructionType { get; set; } = null!;

    public byte? SortOrder { get; set; }

    public bool? IsActive { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual SystemUser? CreatedByNavigation { get; set; }

    public virtual DormitoryCity? DormitoryCity { get; set; }

    public virtual ICollection<HousingInstructionAttachment> HousingInstructionAttachments { get; set; } = new List<HousingInstructionAttachment>();
    
}
