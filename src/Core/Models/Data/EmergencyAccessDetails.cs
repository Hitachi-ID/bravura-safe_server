﻿using Bit.Core.Entities;

namespace Bit.Core.Models.Data;

public class EmergencyAccessDetails : EmergencyAccess
{
    public string GranteeName { get; set; }
    public string GranteeEmail { get; set; }
    public string GranteeAvatarColor { get; set; }
    public string GrantorName { get; set; }
    public string GrantorEmail { get; set; }
    public string GrantorAvatarColor { get; set; }
}
