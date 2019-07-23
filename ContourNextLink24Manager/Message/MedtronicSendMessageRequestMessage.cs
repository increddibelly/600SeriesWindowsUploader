namespace ContourNextLink24Manager.Message
{
    internal class MedtronicSendMessageRequestMessage
    {
        public static MessageType MessageType { get;set;}
    }

    internal interface IMessage
    {
        bool response(int cmd);
        bool response();
    }

    internal class MessageType
    {
        public IMessage READ_PUMP_STATUS { get; internal set; }
        public IMessage READ_HISTORY_INFO { get; internal set; }
        public IMessage READ_BASAL_PATTERN { get; internal set; }
        public IMessage READ_BOLUS_WIZARD_CARB_RATIOS { get; internal set; }
        public IMessage READ_BOLUS_WIZARD_SENSITIVITY_FACTORS { get; internal set; }
        public IMessage NAK_COMMAND { get; internal set; }
        public IMessage EHSM_SESSION { get; internal set; }
        public IMessage INITIATE_MULTIPACKET_TRANSFER { get; internal set; }
        public IMessage MULTIPACKET_SEGMENT_TRANSMISSION { get; internal set; }
        public IMessage END_HISTORY_TRANSMISSION { get; internal set; }
        public IMessage READ_PUMP_TIME { get; internal set; }
        public IMessage READ_BOLUS_WIZARD_BG_TARGETS { get; internal set; }
    }
}