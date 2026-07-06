namespace JHLib.Util.ByteControl
{
    public static class ASCII
    {
        public const byte NULL = 0;
        public const byte START_OF_HEADING = 1;
        public const byte START_OF_TEXT = 2;
        public const byte END_OF_TEXT = 3;
        public const byte END_OF_TRANSMISSION = 4;
        public const byte ENQUIRY = 5;
        public const byte ACKNOWLEDGEMENT = 6;
        public const byte BELL = 7;
        public const byte BACKSPACE = 8;
        public const byte HORIZONTAL_TAB = 9;
        public const byte LINE_FEED = 10;
        public const byte VERTICAL_TAB = 11;
        public const byte FORM_FEED = 12;
        public const byte CARRIAGE_RETURN = 13;
        public const byte SHIFT_OUT = 14;
        public const byte SHIFT_IN = 15;
        public const byte DATA_LINK_ESCAPE = 16;
        public const byte DEVICE_CONTROL_1 = 17;
        public const byte DEVICE_CONTROL_2 = 18;
        public const byte DEVICE_CONTROL_3 = 19;
        public const byte DEVICE_CONTROL_4 = 20;
        public const byte NEGATIVE_ACKNOWLEDGEMENT = 21;
        public const byte SYNCHRONOUS_IDLE = 22;
        public const byte END_OF_TRANSMISSION_BLOCK = 23;
        public const byte CANCEL = 24;
        public const byte END_OF_MEDIUM = 25;
        public const byte SUBSTITUTE = 26;
        public const byte ESCAPE = 27;
        public const byte FILE_SEPARATOR = 28;
        public const byte GROUP_SEPARATOR = 29;
        public const byte RECORD_SEPARATOR = 30;
        public const byte UNIT_SEPARATOR = 31;

        public const byte SPACE = 32;
        /// <summary> ! </summary>
        public const byte EXMARK = 33;
        /// <summary> " </summary>
        public const byte DQUOTE = 34;
        /// <summary> # </summary>
        public const byte SHARP = 35;
        /// <summary> $ </summary>
        public const byte DOLLAR = 36;
        /// <summary> % </summary>
        public const byte PERCENT = 37;
        /// <summary> &amp; </summary>
        public const byte AND = 38;
        /// <summary> ' </summary>
        public const byte SQUOTE = 39;
        /// <summary> ( </summary>
        public const byte LPARENTHESIS = 40;
        /// <summary> ) </summary>
        public const byte RPARENTHESIS = 41;
        /// <summary> * </summary>
        public const byte ASTERISK = 42;
        /// <summary> + </summary>
        public const byte PLUS = 43;
        /// <summary> , </summary>
        public const byte COMMA = 44;
        /// <summary> - </summary>
        public const byte MINUS = 45;
        /// <summary> . </summary>
        public const byte DOT = 46;
        /// <summary> / </summary>
        public const byte SLASH = 47;

        /// <summary> 0 </summary>
        public const byte N0 = 48;
        /// <summary> 1 </summary>
        public const byte N1 = 49;
        /// <summary> 2 </summary>
        public const byte N2 = 50;
        /// <summary> 3 </summary>
        public const byte N3 = 51;
        /// <summary> 4 </summary>
        public const byte N4 = 52;
        /// <summary> 5 </summary>
        public const byte N5 = 53;
        /// <summary> 6 </summary>
        public const byte N6 = 54;
        /// <summary> 7 </summary>
        public const byte N7 = 55;
        /// <summary> 8 </summary>
        public const byte N8 = 56;
        /// <summary> 9 </summary>
        public const byte N9 = 57;

        /// <summary> : </summary>
        public const byte COLON = 58;
        /// <summary> ; </summary>
        public const byte SEMICOLON = 59;
        /// <summary> &lt; </summary>
        public const byte LTHAN = 60;
        /// <summary> = </summary>
        public const byte EQUAL = 61;
        /// <summary> > </summary>
        public const byte GTHAN = 62;
        /// <summary> ? </summary>
        public const byte QUESTION = 63;
        /// <summary> @ </summary>
        public const byte AT = 64;

        public const byte A = 65;
        public const byte B = 66;
        public const byte C = 67;
        public const byte D = 68;
        public const byte E = 69;
        public const byte F = 70;
        public const byte G = 71;
        public const byte H = 72;
        public const byte I = 73;
        public const byte J = 74;
        public const byte K = 75;
        public const byte L = 76;
        public const byte M = 77;
        public const byte N = 78;
        public const byte O = 79;
        public const byte P = 80;
        public const byte Q = 81;
        public const byte R = 82;
        public const byte S = 83;
        public const byte T = 84;
        public const byte U = 85;
        public const byte V = 86;
        public const byte W = 87;
        public const byte X = 88;
        public const byte Y = 89;
        public const byte Z = 90;

        /// <summary> [ </summary>
        public const byte LBRAKET = 91;
        /// <summary> \ </summary>
        public const byte BACKSLASH = 92;
        /// <summary> ] </summary>
        public const byte RBRAKET = 93;
        /// <summary> ^ </summary>
        public const byte CIRCUMFLEX = 94;
        /// <summary> _ </summary>
        public const byte UNDERLINE = 95;
        /// <summary> ` </summary>
        public const byte GRAVE_ACCENT = 96;

        public const byte a = 97;
        public const byte b = 98;
        public const byte c = 99;
        public const byte d = 100;
        public const byte e = 101;
        public const byte f = 102;
        public const byte g = 103;
        public const byte h = 104;
        public const byte i = 105;
        public const byte j = 106;
        public const byte k = 107;
        public const byte l = 108;
        public const byte m = 109;
        public const byte n = 110;
        public const byte o = 111;
        public const byte p = 112;
        public const byte q = 113;
        public const byte r = 114;
        public const byte s = 115;
        public const byte t = 116;
        public const byte u = 117;
        public const byte v = 118;
        public const byte w = 119;
        public const byte x = 120;
        public const byte y = 121;
        public const byte z = 122;

        /// <summary> { </summary>
        public const byte LBRACE = 123;
        /// <summary> | </summary>
        public const byte VERTICAL_BAR = 124;
        /// <summary> } </summary>
        public const byte RBRACE = 125;
        /// <summary> ~ </summary>
        public const byte TILDE = 126;

        /// <summary>
        /// 0~99까지의 숫자를 2자리 ASCII 문자로 변환한 테이블
        /// </summary>
        public static ReadOnlySpan<uint> NumToChar99 =>
        [
        //    0           1           2           3           4           5           6           7           8           9
        /*0*/ 0x00300030, 0x00310030, 0x00320030, 0x00330030, 0x00340030, 0x00350030, 0x00360030, 0x00370030, 0x00380030, 0x00390030,
        /*1*/ 0x00300031, 0x00310031, 0x00320031, 0x00330031, 0x00340031, 0x00350031, 0x00360031, 0x00370031, 0x00380031, 0x00390031,
        /*2*/ 0x00300032, 0x00310032, 0x00320032, 0x00330032, 0x00340032, 0x00350032, 0x00360032, 0x00370032, 0x00380032, 0x00390032,
        /*3*/ 0x00300033, 0x00310033, 0x00320033, 0x00330033, 0x00340033, 0x00350033, 0x00360033, 0x00370033, 0x00380033, 0x00390033,
        /*4*/ 0x00300034, 0x00310034, 0x00320034, 0x00330034, 0x00340034, 0x00350034, 0x00360034, 0x00370034, 0x00380034, 0x00390034,
        /*5*/ 0x00300035, 0x00310035, 0x00320035, 0x00330035, 0x00340035, 0x00350035, 0x00360035, 0x00370035, 0x00380035, 0x00390035,
        /*6*/ 0x00300036, 0x00310036, 0x00320036, 0x00330036, 0x00340036, 0x00350036, 0x00360036, 0x00370036, 0x00380036, 0x00390036,
        /*7*/ 0x00300037, 0x00310037, 0x00320037, 0x00330037, 0x00340037, 0x00350037, 0x00360037, 0x00370037, 0x00380037, 0x00390037,
        /*8*/ 0x00300038, 0x00310038, 0x00320038, 0x00330038, 0x00340038, 0x00350038, 0x00360038, 0x00370038, 0x00380038, 0x00390038,
        /*9*/ 0x00300039, 0x00310039, 0x00320039, 0x00330039, 0x00340039, 0x00350039, 0x00360039, 0x00370039, 0x00380039, 0x00390039
        ];
    }
}