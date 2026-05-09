using System;
using System.Collections.Generic;

namespace UniStay.Models;

public partial class HousingInstructionAttachment
{
    public int ID { get; set; }

    public int HousingInstructionID { get; set; }

    public string FileName { get; set; } = null!;

    public string FilePath { get; set; } = null!;

    public string? FileType { get; set; }

    public byte? SortOrder { get; set; }

    public bool? IsActive { get; set; }

    public virtual HousingInstruction HousingInstruction { get; set; } = null!;
}
