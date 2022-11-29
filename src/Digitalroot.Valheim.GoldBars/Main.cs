using BepInEx;
using BepInEx.Configuration;
using Digitalroot.Valheim.Common;
using JetBrains.Annotations;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using UnityEngine;

namespace Digitalroot.Valheim.GoldBars
{
  [BepInPlugin(Guid, Name, Version)]
  [BepInDependency(Jotunn.Main.ModGuid, "2.9.0")]
  [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
  [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
  public partial class Main : BaseUnityPlugin, ITraceableLogging
  {
    public static Main Instance;

    public static ConfigEntry<int> NexusId;
    public static ConfigEntry<int> CoinPilePieceComfort;
    public static ConfigEntry<int> GoldStackPieceComfort;

    private GameObject _goldBar;
    private GameObject _coinPile;
    private GameObject _coinPilePiece;
    private GameObject _goldStackPiece;
    private AssetBundle _assetBundle;

    public Main()
    {
      Instance = this;
      #if DEBUG
      EnableTrace = true;
      Log.RegisterSource(Instance);
      #else
      EnableTrace = false;
      #endif
      Log.Trace(Main.Instance, $"{Main.Namespace}.{MethodBase.GetCurrentMethod()?.DeclaringType?.Name}.{MethodBase.GetCurrentMethod()?.Name}");
      NexusId = Config.Bind("General", "NexusID", 1448, new ConfigDescription("Nexus mod ID for updates", null, new ConfigurationManagerAttributes { IsAdminOnly = false, Browsable = false, ReadOnly = true }));
      CoinPilePieceComfort = Config.Bind("General", "CoinPilePieceComfort", 1, new ConfigDescription("Coin Pile Comfort Level", null, new ConfigurationManagerAttributes { IsAdminOnly = true }));
      GoldStackPieceComfort = Config.Bind("General", "GoldStackPieceComfort", 2, new ConfigDescription("Gold Stack Comfort Level", null, new ConfigurationManagerAttributes { IsAdminOnly = true }));
    }

    [UsedImplicitly]
    private void Awake()
    {
      try
      {
        Log.Trace(Main.Instance, $"{Main.Namespace}.{MethodBase.GetCurrentMethod()?.DeclaringType?.Name}.{MethodBase.GetCurrentMethod()?.Name}");
        _assetBundle = AssetUtils.LoadAssetBundleFromResources("goldbar", typeof(Main).Assembly);

        #if DEBUG
        foreach (var assetName in _assetBundle.GetAllAssetNames())
        {
          Log.Trace(Instance, assetName);
        }
        #endif

        LoadPrefabs();
        LoadRecipes();
        LoadConversion();
        LoadPieces();

        _assetBundle.Unload(false);
      }
      catch (Exception e)
      {
        Log.Error(Instance, e);
      }
    }

    private void LoadPrefabs()
    {
      _goldBar = _assetBundle.LoadAsset<GameObject>("assets/goldingot/goldingot.prefab");
      ItemManager.Instance.AddItem(new CustomItem(_goldBar, false));

      _coinPile = _assetBundle.LoadAsset<GameObject>("assets/goldingot/coinpile.prefab");
      ItemManager.Instance.AddItem(new CustomItem(_coinPile, false, new ItemConfig
      {
        Amount = 1, Requirements = new[]
        {
          new RequirementConfig
          {
            Item = "Coins", Amount = 200
          }
        }
      }));
    }

    private void LoadRecipes()
    {
      CustomRecipe coinRecipe = new CustomRecipe(new RecipeConfig
      {
        Amount = 200, Item = "Coins", // Name of the item prefab to be crafted
        Name = "PileToCoins"
        , Requirements = new[] // Resources and amount needed for it to be crafted
        {
          new RequirementConfig { Item = "CoinPile", Amount = 1 },
        }
      });
      ItemManager.Instance.AddRecipe(coinRecipe);

      CustomRecipe coinRecipe2 = new CustomRecipe(new RecipeConfig
      {
        Amount = 100, Item = "Coins", CraftingStation = "forge", Name = "GoldIngotToCoins", MinStationLevel = 2, Requirements = new[] // Resources and amount needed for it to be crafted
        {
          new RequirementConfig { Item = "GoldIngot", Amount = 1 }, new RequirementConfig { Item = "Copper", Amount = 1 },
        }
      });
      ItemManager.Instance.AddRecipe(coinRecipe2);
    }

    private void LoadConversion()
    {
      var smeltConversion = new CustomItemConversion(new SmelterConversionConfig
      {
        FromItem = "CoinPile", ToItem = "GoldIngot"
      });
      ItemManager.Instance.AddItemConversion(smeltConversion);
    }

    private void LoadPieces()
    {
      AddGoldStack();
      AddCoinPile();
    }

    private void AddCoinPile()
    {
      _coinPilePiece = _assetBundle.LoadAsset<GameObject>("assets/goldingot/piece_coinpile.prefab");
      #if DEBUG
      Log.Trace(Instance, $"_coinPilePiece == null : {_coinPilePiece == null}"); // This is null?
      #endif

      var coinPilePiece = new CustomPiece(_coinPilePiece,
                                          false,
                                          new PieceConfig
                                          {
                                            PieceTable = "_HammerPieceTable", CraftingStation = "", Enabled = true, Requirements = new[]
                                            {
                                              new RequirementConfig { Item = "Coins", Amount = 200, Recover = true },
                                            }
                                          })
      {
        Piece =
        {
          m_comfort = CoinPilePieceComfort.Value
        }
      };
      PieceManager.Instance.AddPiece(coinPilePiece);
    }

    private void AddGoldStack()
    {
      _goldStackPiece = _assetBundle.LoadAsset<GameObject>("assets/goldingot/piece_goldstack.prefab");
      #if DEBUG
      Log.Trace(Instance, $"_goldStackPiece == null : {_goldStackPiece == null}"); // This is null?
      #endif

      var goldBarStack = new CustomPiece(_goldStackPiece,
                                         false,
                                         new PieceConfig
                                         {
                                           PieceTable = "_HammerPieceTable", CraftingStation = "", Enabled = true, Requirements = new[]
                                           {
                                             new RequirementConfig { Item = "GoldIngot", Amount = 48, Recover = true },
                                           }
                                           ,
                                         })
      {
        Piece =
        {
          m_comfort = GoldStackPieceComfort.Value
        }
      };
      PieceManager.Instance.AddPiece(goldBarStack);
    }

    #region Implementation of ITraceableLogging

    /// <inheritdoc />
    public string Source => Namespace;

    /// <inheritdoc />
    public bool EnableTrace { get; }

    #endregion
  }
}
