using System;

namespace ClientApp // انتبه: في مشروع العميل سيكون الاسم ClientApp
{
    public class MessagePacket
    {
        public string SenderName { get; set; }   // اسم الطالب/المرسل
        public string Type { get; set; }         // نوع البيانات: "Text" أو "File"
        public string Content { get; set; }      // نص الرسالة أو اسم الملف
        public byte[] FileData { get; set; }     // بيانات الملف (مصفوفة بايتات)
    }
}