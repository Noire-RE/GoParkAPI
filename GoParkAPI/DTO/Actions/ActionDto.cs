namespace GoParkAPI.DTO.Actions
{
    public class ActionDto
    {
        public string Type { get; set; }
        public string? Label { get; set; }

        //--------------1. Postback action.
        public string? Data { get; set; }
        public string? DisplayText { get; set; }

        public string? InputOption { get; set; } // 有四個選項:Open/close rich menu、Open Keyboard(開啟手機鍵盤)、Open Voice(開啟錄音功能)

        public string? FillInText { get; set; } //當InputOption帶入OpenKeyboard 時，可在這裡預先填好開啟鍵盤後的文字內容。

        //--------------2. Message Action:讓使用者傳送設定好的文字訊息到line bot聊天室，這樣就可以透過 Webhook 接收事件用收到的關鍵字對使用者作出回應

        public string? Text { get; set; }

        //--------------3. Uri action.

        public string? Uri { get; set; }
        public UriActionAltUriDto? AltUri { get; set; } //設定在不同裝置開啟的URI(會取代上面的URI)

        // -------------4. datetime picker action
        public string? Mode { get; set; } //設定時間選擇器的模式，可分為 選擇時間(time)、日期(date)、日期時間(datetime)。
        public string? Initial { get; set; }
        public string? Max { get; set; }
        public string? Min { get; set; }





        //對應到上方3.
        public class UriActionAltUriDto
        {
            public string Desktop { get; set; }
        }

    }
}
