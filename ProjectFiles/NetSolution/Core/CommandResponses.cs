namespace NETCode.Core
{
    public static class CommandResponseCodes
    {
        public const int NoAction = 0;
        public const int Success = 1;
        public const int Rework = 2;
        public const int PalletNotFound = 901;
        public const int VariantNotFound = 902;
        public const int UnitAlreadyExists = 903;
        public const int InvalidIdOrValidationCode = 904;
        public const short UnitNotAtThisStation = 905;
        public const short UnitNotFound = 906;
        public const short NoOperationsFound = 907;
        public const short RecipeNotFound = 908;
        public const short OperationFailed = 909;
        public const short NoLastStation = 910;
        public const int GeneralError = 999;
    }
}
