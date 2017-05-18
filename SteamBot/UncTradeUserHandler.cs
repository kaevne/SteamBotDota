using SteamKit2;
using System.Collections.Generic;
using System;
using System.Globalization;
using SteamTrade;
using System.IO;

namespace SteamBot
{
    public class UncTradeUserHandler : BaseUserHandler
    {
        public UncTradeUserHandler(Bot bot, SteamID sid)
            : base(bot, sid)
        {
            OTHER_B_TO_A = bot.OTHER_B_TO_A;
            OTHER_A_TO_B = bot.OTHER_A_TO_B;
            OTHER_MAX_A = 3;
            MY_MAX_A = 3;     

            OTHER_MAX_B = OTHER_B_TO_A * MY_MAX_A - 1;
            MY_MAX_B = OTHER_A_TO_B * OTHER_MAX_A + 1;
            
            ItemADesc = "Rare";
            ItemBDesc = "Uncommon";
            uniqueOnlyA = false;
            uniqueOnlyB = true;
        }

        public override bool isQualifiedItemA(Schema.Item schemaItem)
        {
            if (schemaItem.Rarity == "Rare" && schemaItem.TrueName == "Standard")
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public override bool isQualifiedItemB(Schema.Item schemaItem)
        {
            if (schemaItem.Rarity == "Uncommon" && schemaItem.TrueName == "Standard")
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        protected override bool validateA(Schema.Item schemaItem)
        {
            if (!isQualifiedItemA(schemaItem))
            {
                sendBothMessage("Sorry, item you offered: " + schemaItem.ItemName + " is not a " + ItemADesc + " and Standard quality.");
                return false;
            }
            else if (schemaItem.ItemClass != "dota_item_wearable")
            {
                sendBothMessage("Sorry, item you offered: " + schemaItem.ItemName + " is not a valid dota item that we are accepting.");
                return false;
            }
            else if (schemaItem.ItemTypeName.ToLower().Contains("taunt"))
            {
                sendBothMessage("Sorry, item you offered: " + schemaItem.ItemName + " is not a valid dota item that we are accepting.");
                return false;
            }

            return true;
        }

        protected override bool validateB(Schema.Item schemaItem, Inventory.Item item)
        {
            if (!isQualifiedItemB(schemaItem))
            {
                sendBothMessage("Sorry, item you offered: " + schemaItem.ItemName + " is not a " + ItemBDesc + " and Standard quality.");
                return false;
            }
            else if (schemaItem.ItemClass != "dota_item_wearable")
            {
                sendBothMessage("Sorry, item you offered: " + schemaItem.ItemName + " is not a valid dota item that we are accepting.");
                return false;
            }
            else if (schemaItem.ItemTypeName.ToLower().Contains("taunt"))
            {
                sendBothMessage("Sorry, item you offered: " + schemaItem.ItemName + " is not a valid dota item that we are accepting.");
                return false;
            }
            return true;
        }

    }

}

