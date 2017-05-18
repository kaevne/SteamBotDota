using SteamKit2;
using System.Collections.Generic;
using System;
using System.Globalization;
using SteamTrade;
using System.IO;

namespace SteamBot
{
    public class RareTradeUserHandler : BaseUserHandler
    {
        const int TREASURE_KEY_INDEX = 15003;


        public RareTradeUserHandler(Bot bot, SteamID sid)
            : base(bot, sid)
        {
            OTHER_B_TO_A = bot.OTHER_B_TO_A;
            OTHER_A_TO_B = bot.OTHER_A_TO_B;
            OTHER_MAX_A = 3;
            MY_MAX_A = 3;

            OTHER_MAX_B = OTHER_B_TO_A * MY_MAX_A - 1;
            MY_MAX_B = OTHER_A_TO_B * OTHER_MAX_A + 1;

            ItemADesc = "Key";
            ItemBDesc = "Rare";
            uniqueOnlyA = false;
            uniqueOnlyB = true;
        }

        public override bool isQualifiedItemA(Schema.Item schemaItem)
        {
            if (schemaItem.Defindex == TREASURE_KEY_INDEX)
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
            if (schemaItem.Rarity == "Rare" && schemaItem.TrueName == "Standard")
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
                sendBothMessage("Sorry, item you offered: " + schemaItem.ItemName + " is not a " + ItemADesc + ".");
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

        protected bool isGifted(Inventory.Item item)
        {

            bool isGifted = false;
            try
            {
                foreach (Inventory.ItemAttribute attribute in item.Attributes)
                {
                    Log.Debug("attributetostring: " + attribute.ToString());
                    Log.Debug("defindex: " + attribute.Defindex);
                    if (attribute.Defindex == 186)
                    {
                        isGifted = true;
                    }
                }
            }
            catch
            {
                isGifted = false;
            }
            return isGifted;
        }

    }

}

