using System;
using System.Collections.Generic;
using BazaarGameShared.Domain.Core;
using BazaarGameShared.Domain.Core.Types;

namespace BazaarPlannerMod;

public class RunInfo
{
    public String Character; 
    public List<SkillInfo> Skills;
    public List<SkillInfo> OppSkills;
    public List<CardInfo> Cards; 
    public List<CardInfo> Stash; 
    public List<CardInfo> OppCards;
    public List<CardInfo> OppStash;
    public int? OppHealth;
    public int? OppRegen;
    public uint Wins;
    public int Day;
    public string Version = "0.0.1";
    public int? Health;
    public int? Regen { get; set; }
    public int? Level { get; set; }
    public string Name;


    public class SkillInfo
    {
        public ETier Tier;
        public Guid TemplateId;
        public string Name;
    }
    public class CardInfo
    {
        public ETier Tier;
        public string Name;
        public Guid TemplateId;
        public EContainerSocketId? Left;
        public InstanceId Instance;
        public Dictionary<ECardAttributeType, int> Attributes { get; set; } = new Dictionary<ECardAttributeType, int>();
    }
    
}