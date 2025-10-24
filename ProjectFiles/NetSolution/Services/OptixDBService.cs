using NETCode.Repositories;
using NETCode.Services;

public class OptixDBService
{
    public OperationRepository OperationRepo { get; private set; }
    public StationRepository StationRepo { get; private set; }
    public StationRoutesRepository StationRoutesRepo { get; private set; }
    public PickToLightBinRepository PickToLightBinRepo { get; private set; }
    public OperationTypeRepository OperationTypeRepo { get; private set; }
    public UserRepository UsersRepo { get; private set; }
    public VariantsRepository VariantsRepo { get; private set; }
    public OperationBehaviorRepository OperationBehaviorRepo { get; private set; }
    public RecipeRepository RecipeRepo { get; private set; }
    public ProductionUnitRepository ProductionUnitRepo { get; set; }
    public ProductionUnitResultRepository ProductionUnitStationResultRepo { get; set; }
    public ProductionUnitOperationResultRepository ProductionUnitOperationResultRepo { get; set; }
    public PalletRepository PalletRepo { get; set; }

    private static OptixDBService _instance;

    private OptixDBService()
    {

        OperationRepo = new OperationRepository();
        StationRepo = new StationRepository();
        StationRoutesRepo = new StationRoutesRepository();
        OperationTypeRepo = new OperationTypeRepository();
        UsersRepo = new UserRepository();
        VariantsRepo = new VariantsRepository();
        OperationBehaviorRepo = new OperationBehaviorRepository();
        RecipeRepo = new RecipeRepository();
        ProductionUnitRepo = new ProductionUnitRepository();
        PalletRepo = new PalletRepository();
        ProductionUnitStationResultRepo = new ProductionUnitResultRepository();
        ProductionUnitOperationResultRepo = new ProductionUnitOperationResultRepository();
        PickToLightBinRepo   = new PickToLightBinRepository();

    }

    public static OptixDBService GetInstance()
    {
        if (_instance == null)
            _instance = new OptixDBService();
        return _instance;
    }
}
