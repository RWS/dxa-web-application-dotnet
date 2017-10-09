using System;

namespace Sdl.Web.Tridion.ContentManager
{
    /// <summary>
    /// Defines ConstantsCommon for system defined item types.
    /// </summary>
    internal class ItemTypes
    {
        internal const int baseSystemWideItems = 0x10000;
        internal const int baseWorkflowItems = 0x20000;
    }

    /// <summary>
    /// Specifies the Content Manager item types.
    /// </summary>
    /// <remarks>
    /// This enumeration defines both the symbolic names and numeric values for all TCM item types.
    /// The numeric values are used in TCM URIs.
    /// Note that the numeric values look like bit flags, but they are not really: 
    /// repository-local, system-wide and workflow item types have overlapping bit values.
    /// Therefore, performing bitwise ORs or ANDs is dangerous; it can only be done for item types of
    /// the same "class" (repository-local, system-wide or workflow).
    /// </remarks>
    public enum ItemType
    {
#pragma warning disable 1591 // Disable missing XML comment warning
        None = 0x0,
        Publication = 0x1,
        Folder = 0x2,
        StructureGroup = 0x4,
        Schema = 0x8,
        Component = 0x10,
        ComponentTemplate = 0x20,
        Page = 0x40,
        PageTemplate = 0x80,
        TargetGroup = 0x100,
        Category = 0x200,
        Keyword = 0x400,
        TemplateBuildingBlock = 0x800,
        VirtualFolder = 0x2000,
        PublicationTarget = ItemTypes.baseSystemWideItems + 0x1,
        TargetType = ItemTypes.baseSystemWideItems + 0x2,
        TargetDestination = ItemTypes.baseSystemWideItems + 0x4,
        MultimediaType = ItemTypes.baseSystemWideItems + 0x8,
        User = ItemTypes.baseSystemWideItems + 0x10,
        Group = ItemTypes.baseSystemWideItems + 0x20,
        DirectoryService = ItemTypes.baseSystemWideItems + 0x80,
        DirectoryGroupMapping = ItemTypes.baseSystemWideItems + 0x100,
        Batch = ItemTypes.baseSystemWideItems + 0x200,

        
        [Obsolete]
        MultipleOperations = ItemTypes.baseSystemWideItems + 0x200,

        PublishTransaction = ItemTypes.baseSystemWideItems + 0x400,
        WorkflowType = ItemTypes.baseSystemWideItems + 0x800,
        ApprovalStatus = ItemTypes.baseWorkflowItems + 0x1,
        ProcessDefinition = ItemTypes.baseWorkflowItems + 0x2,
        ProcessInstance = ItemTypes.baseWorkflowItems + 0x4,
        ProcessHistory = ItemTypes.baseWorkflowItems + 0x8,
        ActivityDefinition = ItemTypes.baseWorkflowItems + 0x10,
        ActivityInstance = ItemTypes.baseWorkflowItems + 0x20,
        ActivityHistory = ItemTypes.baseWorkflowItems + 0x40,
        WorkItem = ItemTypes.baseWorkflowItems + 0x80
#pragma warning restore 1591
    }
}
