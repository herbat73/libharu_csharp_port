namespace LibHaru;

public sealed class HaruException : Exception
{
    public HaruException(uint status, string message)
        : this(status, HaruStatus.NoError, message)
    {
    }

    public HaruException(uint status, uint detailStatus, string message)
        : base(message)
    {
        Status = status;
        DetailStatus = detailStatus;
    }

    public uint Status { get; }

    public uint DetailStatus { get; }
}

public delegate void HaruErrorHandler(uint errorNo, uint detailNo, object? userData);

public static class HaruStatus
{
    public const uint OK = 0;
    public const uint NoError = 0;

    public const uint ArrayCountErr = 0x1001;
    public const uint ArrayItemNotFound = 0x1002;
    public const uint ArrayItemUnexpectedType = 0x1003;
    public const uint BinaryLengthErr = 0x1004;
    public const uint CannotGetPallet = 0x1005;
    public const uint DictCountErr = 0x1007;
    public const uint DictItemNotFound = 0x1008;
    public const uint DictItemUnexpectedType = 0x1009;
    public const uint DictStreamLengthNotFound = 0x100A;
    public const uint DocEncryptDictNotFound = 0x100B;
    public const uint DocInvalidObject = 0x100C;
    public const uint DuplicateRegistration = 0x100E;
    public const uint ExceedJwwCodeNumLimit = 0x100F;
    public const uint EncryptInvalidPassword = 0x1011;
    public const uint ErrUnknownClass = 0x1013;
    public const uint ExceedGstateLimit = 0x1014;
    public const uint FailedToAllocMem = 0x1015;
    public const uint FileIoError = 0x1016;
    public const uint FileOpenError = 0x1017;
    public const uint FontExists = 0x1019;
    public const uint FontInvalidWidthsTable = 0x101A;
    public const uint InvalidAfmHeader = 0x101B;
    public const uint InvalidAnnotation = 0x101C;
    public const uint InvalidBitPerComponent = 0x101E;
    public const uint InvalidCharMatricsData = 0x101F;
    public const uint InvalidColorSpace = 0x1020;
    public const uint InvalidCompressionMode = 0x1021;
    public const uint InvalidDateTime = 0x1022;
    public const uint InvalidDestination = 0x1023;
    public const uint InvalidDocument = 0x1025;
    public const uint InvalidDocumentState = 0x1026;
    public const uint InvalidEncoder = 0x1027;
    public const uint InvalidEncoderType = 0x1028;
    public const uint InvalidEncodingName = 0x102B;
    public const uint InvalidEncryptKeyLen = 0x102C;
    public const uint InvalidFontDefData = 0x102D;
    public const uint InvalidFontDefType = 0x102E;
    public const uint InvalidFontName = 0x102F;
    public const uint InvalidImage = 0x1030;
    public const uint InvalidJpegData = 0x1031;
    public const uint InvalidNData = 0x1032;
    public const uint InvalidObject = 0x1033;
    public const uint InvalidObjId = 0x1034;
    public const uint InvalidOperation = 0x1035;
    public const uint InvalidOutline = 0x1036;
    public const uint InvalidPage = 0x1037;
    public const uint InvalidPages = 0x1038;
    public const uint InvalidParameter = 0x1039;
    public const uint InvalidPngImage = 0x103B;
    public const uint InvalidStream = 0x103C;
    public const uint MissingFileNameEntry = 0x103D;
    public const uint InvalidTtcFile = 0x103F;
    public const uint InvalidTtcIndex = 0x1040;
    public const uint InvalidWxData = 0x1041;
    public const uint ItemNotFound = 0x1042;
    public const uint LibPngError = 0x1043;
    public const uint NameInvalidValue = 0x1044;
    public const uint NameOutOfRange = 0x1045;
    public const uint PageInvalidParamCount = 0x1048;
    public const uint PagesMissingKidsEntry = 0x1049;
    public const uint PageCannotFindObject = 0x104A;
    public const uint PageCannotGetRootPages = 0x104B;
    public const uint PageCannotRestoreGstate = 0x104C;
    public const uint PageCannotSetParent = 0x104D;
    public const uint PageFontNotFound = 0x104E;
    public const uint PageInvalidFont = 0x104F;
    public const uint PageInvalidFontSize = 0x1050;
    public const uint PageInvalidGmode = 0x1051;
    public const uint PageInvalidIndex = 0x1052;
    public const uint PageInvalidRotateValue = 0x1053;
    public const uint PageInvalidSize = 0x1054;
    public const uint PageInvalidXObject = 0x1055;
    public const uint PageOutOfRange = 0x1056;
    public const uint RealOutOfRange = 0x1057;
    public const uint StreamEof = 0x1058;
    public const uint StreamReadLnContinue = 0x1059;
    public const uint StringOutOfRange = 0x105B;
    public const uint ThisFuncWasSkipped = 0x105C;
    public const uint TtfCannotEmbeddingFont = 0x105D;
    public const uint TtfInvalidCmap = 0x105E;
    public const uint TtfInvalidFormat = 0x105F;
    public const uint TtfMissingTable = 0x1060;
    public const uint UnsupportedFontType = 0x1061;
    public const uint UnsupportedFunction = 0x1062;
    public const uint UnsupportedJpegFormat = 0x1063;
    public const uint UnsupportedType1Font = 0x1064;
    public const uint XrefCountErr = 0x1065;
    public const uint ZlibError = 0x1066;
    public const uint InvalidPageIndex = 0x1067;
    public const uint InvalidUri = 0x1068;
    public const uint PageLayoutOutOfRange = 0x1069;
    public const uint PageModeOutOfRange = 0x1070;
    public const uint PageNumStyleOutOfRange = 0x1071;
    public const uint AnnotInvalidIcon = 0x1072;
    public const uint AnnotInvalidBorderStyle = 0x1073;
    public const uint PageInvalidDirection = 0x1074;
    public const uint InvalidFont = 0x1075;
    public const uint PageInsufficientSpace = 0x1076;
    public const uint PageInvalidDisplayTime = 0x1077;
    public const uint PageInvalidTransitionTime = 0x1078;
    public const uint InvalidPageSlideshowType = 0x1079;
    public const uint ExtGStateOutOfRange = 0x1080;
    public const uint InvalidExtGState = 0x1081;
    public const uint ExtGStateReadOnly = 0x1082;
    public const uint InvalidU3DData = 0x1083;
    public const uint NameCannotGetNames = 0x1084;
    public const uint InvalidIccComponentNum = 0x1085;
    public const uint PageInvalidBoundary = 0x1086;
    public const uint InvalidShadingType = 0x1088;

    public const uint HPDF_ARRAY_COUNT_ERR = ArrayCountErr;
    public const uint HPDF_ARRAY_ITEM_NOT_FOUND = ArrayItemNotFound;
    public const uint HPDF_ARRAY_ITEM_UNEXPECTED_TYPE = ArrayItemUnexpectedType;
    public const uint HPDF_BINARY_LENGTH_ERR = BinaryLengthErr;
    public const uint HPDF_CANNOT_GET_PALLET = CannotGetPallet;
    public const uint HPDF_DICT_COUNT_ERR = DictCountErr;
    public const uint HPDF_DICT_ITEM_NOT_FOUND = DictItemNotFound;
    public const uint HPDF_DICT_ITEM_UNEXPECTED_TYPE = DictItemUnexpectedType;
    public const uint HPDF_DICT_STREAM_LENGTH_NOT_FOUND = DictStreamLengthNotFound;
    public const uint HPDF_DOC_ENCRYPTDICT_NOT_FOUND = DocEncryptDictNotFound;
    public const uint HPDF_DOC_INVALID_OBJECT = DocInvalidObject;
    public const uint HPDF_DUPLICATE_REGISTRATION = DuplicateRegistration;
    public const uint HPDF_EXCEED_JWW_CODE_NUM_LIMIT = ExceedJwwCodeNumLimit;
    public const uint HPDF_ENCRYPT_INVALID_PASSWORD = EncryptInvalidPassword;
    public const uint HPDF_ERR_UNKNOWN_CLASS = ErrUnknownClass;
    public const uint HPDF_EXCEED_GSTATE_LIMIT = ExceedGstateLimit;
    public const uint HPDF_FAILED_TO_ALLOC_MEM = FailedToAllocMem;
    public const uint HPDF_FAILD_TO_ALLOC_MEM = FailedToAllocMem;
    public const uint HPDF_FILE_IO_ERROR = FileIoError;
    public const uint HPDF_FILE_OPEN_ERROR = FileOpenError;
    public const uint HPDF_FONT_EXISTS = FontExists;
    public const uint HPDF_FONT_INVALID_WIDTHS_TABLE = FontInvalidWidthsTable;
    public const uint HPDF_INVALID_AFM_HEADER = InvalidAfmHeader;
    public const uint HPDF_INVALID_ANNOTATION = InvalidAnnotation;
    public const uint HPDF_INVALID_BIT_PER_COMPONENT = InvalidBitPerComponent;
    public const uint HPDF_INVALID_CHAR_MATRICS_DATA = InvalidCharMatricsData;
    public const uint HPDF_INVALID_COLOR_SPACE = InvalidColorSpace;
    public const uint HPDF_INVALID_COMPRESSION_MODE = InvalidCompressionMode;
    public const uint HPDF_INVALID_DATE_TIME = InvalidDateTime;
    public const uint HPDF_INVALID_DESTINATION = InvalidDestination;
    public const uint HPDF_INVALID_DOCUMENT = InvalidDocument;
    public const uint HPDF_INVALID_DOCUMENT_STATE = InvalidDocumentState;
    public const uint HPDF_INVALID_ENCODER = InvalidEncoder;
    public const uint HPDF_INVALID_ENCODER_TYPE = InvalidEncoderType;
    public const uint HPDF_INVALID_ENCODING_NAME = InvalidEncodingName;
    public const uint HPDF_INVALID_ENCRYPT_KEY_LEN = InvalidEncryptKeyLen;
    public const uint HPDF_INVALID_FONTDEF_DATA = InvalidFontDefData;
    public const uint HPDF_INVALID_FONTDEF_TYPE = InvalidFontDefType;
    public const uint HPDF_INVALID_FONT_NAME = InvalidFontName;
    public const uint HPDF_INVALID_IMAGE = InvalidImage;
    public const uint HPDF_INVALID_JPEG_DATA = InvalidJpegData;
    public const uint HPDF_INVALID_N_DATA = InvalidNData;
    public const uint HPDF_INVALID_OBJECT = InvalidObject;
    public const uint HPDF_INVALID_OBJ_ID = InvalidObjId;
    public const uint HPDF_INVALID_OPERATION = InvalidOperation;
    public const uint HPDF_INVALID_OUTLINE = InvalidOutline;
    public const uint HPDF_INVALID_PAGE = InvalidPage;
    public const uint HPDF_INVALID_PAGES = InvalidPages;
    public const uint HPDF_INVALID_PARAMETER = InvalidParameter;
    public const uint HPDF_INVALID_PNG_IMAGE = InvalidPngImage;
    public const uint HPDF_INVALID_STREAM = InvalidStream;
    public const uint HPDF_MISSING_FILE_NAME_ENTRY = MissingFileNameEntry;
    public const uint HPDF_INVALID_TTC_FILE = InvalidTtcFile;
    public const uint HPDF_INVALID_TTC_INDEX = InvalidTtcIndex;
    public const uint HPDF_INVALID_WX_DATA = InvalidWxData;
    public const uint HPDF_ITEM_NOT_FOUND = ItemNotFound;
    public const uint HPDF_LIBPNG_ERROR = LibPngError;
    public const uint HPDF_NAME_INVALID_VALUE = NameInvalidValue;
    public const uint HPDF_NAME_OUT_OF_RANGE = NameOutOfRange;
    public const uint HPDF_PAGE_INVALID_PARAM_COUNT = PageInvalidParamCount;
    public const uint HPDF_PAGES_MISSING_KIDS_ENTRY = PagesMissingKidsEntry;
    public const uint HPDF_PAGE_CANNOT_FIND_OBJECT = PageCannotFindObject;
    public const uint HPDF_PAGE_CANNOT_GET_ROOT_PAGES = PageCannotGetRootPages;
    public const uint HPDF_PAGE_CANNOT_RESTORE_GSTATE = PageCannotRestoreGstate;
    public const uint HPDF_PAGE_CANNOT_SET_PARENT = PageCannotSetParent;
    public const uint HPDF_PAGE_FONT_NOT_FOUND = PageFontNotFound;
    public const uint HPDF_PAGE_INVALID_FONT = PageInvalidFont;
    public const uint HPDF_PAGE_INVALID_FONT_SIZE = PageInvalidFontSize;
    public const uint HPDF_PAGE_INVALID_GMODE = PageInvalidGmode;
    public const uint HPDF_PAGE_INVALID_INDEX = PageInvalidIndex;
    public const uint HPDF_PAGE_INVALID_ROTATE_VALUE = PageInvalidRotateValue;
    public const uint HPDF_PAGE_INVALID_SIZE = PageInvalidSize;
    public const uint HPDF_PAGE_INVALID_XOBJECT = PageInvalidXObject;
    public const uint HPDF_PAGE_OUT_OF_RANGE = PageOutOfRange;
    public const uint HPDF_REAL_OUT_OF_RANGE = RealOutOfRange;
    public const uint HPDF_STREAM_EOF = StreamEof;
    public const uint HPDF_STREAM_READLN_CONTINUE = StreamReadLnContinue;
    public const uint HPDF_STRING_OUT_OF_RANGE = StringOutOfRange;
    public const uint HPDF_THIS_FUNC_WAS_SKIPPED = ThisFuncWasSkipped;
    public const uint HPDF_TTF_CANNOT_EMBEDDING_FONT = TtfCannotEmbeddingFont;
    public const uint HPDF_TTF_INVALID_CMAP = TtfInvalidCmap;
    public const uint HPDF_TTF_INVALID_FOMAT = TtfInvalidFormat;
    public const uint HPDF_TTF_MISSING_TABLE = TtfMissingTable;
    public const uint HPDF_UNSUPPORTED_FONT_TYPE = UnsupportedFontType;
    public const uint HPDF_UNSUPPORTED_FUNC = UnsupportedFunction;
    public const uint HPDF_UNSUPPORTED_JPEG_FORMAT = UnsupportedJpegFormat;
    public const uint HPDF_UNSUPPORTED_TYPE1_FONT = UnsupportedType1Font;
    public const uint HPDF_XREF_COUNT_ERR = XrefCountErr;
    public const uint HPDF_ZLIB_ERROR = ZlibError;
    public const uint HPDF_INVALID_PAGE_INDEX = InvalidPageIndex;
    public const uint HPDF_INVALID_URI = InvalidUri;
    public const uint HPDF_PAGE_LAYOUT_OUT_OF_RANGE = PageLayoutOutOfRange;
    public const uint HPDF_PAGE_MODE_OUT_OF_RANGE = PageModeOutOfRange;
    public const uint HPDF_PAGE_NUM_STYLE_OUT_OF_RANGE = PageNumStyleOutOfRange;
    public const uint HPDF_ANNOT_INVALID_ICON = AnnotInvalidIcon;
    public const uint HPDF_ANNOT_INVALID_BORDER_STYLE = AnnotInvalidBorderStyle;
    public const uint HPDF_PAGE_INVALID_DIRECTION = PageInvalidDirection;
    public const uint HPDF_INVALID_FONT = InvalidFont;
    public const uint HPDF_PAGE_INSUFFICIENT_SPACE = PageInsufficientSpace;
    public const uint HPDF_PAGE_INVALID_DISPLAY_TIME = PageInvalidDisplayTime;
    public const uint HPDF_PAGE_INVALID_TRANSITION_TIME = PageInvalidTransitionTime;
    public const uint HPDF_INVALID_PAGE_SLIDESHOW_TYPE = InvalidPageSlideshowType;
    public const uint HPDF_EXT_GSTATE_OUT_OF_RANGE = ExtGStateOutOfRange;
    public const uint HPDF_INVALID_EXT_GSTATE = InvalidExtGState;
    public const uint HPDF_EXT_GSTATE_READ_ONLY = ExtGStateReadOnly;
    public const uint HPDF_INVALID_U3D_DATA = InvalidU3DData;
    public const uint HPDF_NAME_CANNOT_GET_NAMES = NameCannotGetNames;
    public const uint HPDF_INVALID_ICC_COMPONENT_NUM = InvalidIccComponentNum;
    public const uint HPDF_PAGE_INVALID_BOUNDARY = PageInvalidBoundary;
    public const uint HPDF_INVALID_SHADING_TYPE = InvalidShadingType;

    public const uint UnsupportedFeature = UnsupportedFunction;
}
