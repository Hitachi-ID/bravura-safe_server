﻿using Bit.Core.SecretsManager.Entities;

namespace Bit.Core.SecretsManager.Models.Data;

public class ProjectPermissionDetails : Project
{
    public bool Read { get; set; }
    public bool Write { get; set; }
}
