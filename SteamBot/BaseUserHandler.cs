using SteamKit2;
using System.Collections.Generic;
using System;
using System.Globalization;
using SteamTrade;
using System.IO;
using System.Linq;


namespace SteamBot
{
    public class BaseUserHandler : UserHandler
    {
        protected Stack<Schema.Item> PossibleItemA;
        protected Stack<Schema.Item> PossibleItemB;
        protected Stack<Schema.Item> OfferedItems;

        protected int friendsListMax = 250;

        protected int OTHER_B_TO_A;
        protected int OTHER_A_TO_B;
        protected int OTHER_MAX_B;
        protected int OTHER_MAX_A;
        protected int MY_MAX_A;
        protected int MY_MAX_B;
        protected string ItemADesc;
        protected string ItemBDesc;
        protected bool uniqueOnlyA;
        protected bool uniqueOnlyB;

        protected enum TradeMode
        {
            OtherItemA,    // User is offering A
            OtherItemB // User is offering B
        }
        protected TradeMode Mode;
        protected int OfferedItemsCount;
        protected dynamic myRawJsonInventory;
        private List<ushort> whitelist;

        public BaseUserHandler(Bot bot, SteamID sid)
            : base(bot, sid)
        {
            Bot.GetInventory();
            Mode = TradeMode.OtherItemA;

            whitelist = new List<ushort>();
            string filename = "whitelist.txt";
            if (File.Exists(filename))
            {
                TextReader reader = new StreamReader(filename);
                while (reader.Peek() != -1)
                {
                    string line = reader.ReadLine().Trim();
                    if (line.Length > 0)
                    {
                        whitelist.Add(Convert.ToUInt16(line));
                    }
                }
                Log.Info("Loaded Whitelist.  " + whitelist.ToArray().Length + " items whitelisted.");
            }
            else
            {
                Log.Warn("whitelist.txt not found.");
            }
            
            //remove friendrequests while we were offline
            removeFriendRequests();
        }

        public void sendBothMessage(String message)
        {
            Trade.SendMessage(message);
            Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg, message);
        }

        public virtual bool isQualifiedItemA(Schema.Item schemaItem)
        {
            return true;
        }

        public virtual bool isQualifiedItemB(Schema.Item schemaItem)
        {
            return true;
        }


        public override void OnTradeSuccess()
        {
        }

        public override bool OnGroupAdd()
        {
            return false;
        }
        public override bool OnFriendAdd()
        {
            string renderedSteamID = OtherSID.Render();
            if (!renderedSteamID.ToLower().StartsWith("steam_"))
            {
                //invited to a group, not by a friend
                return false;
            }
            else
            {
                Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg, Bot.ChatResponse);

                if (Bot.CurrentTrade == null)
                {
                    Bot.SteamTrade.Trade(OtherSID);
                }
                else
                {
                    Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg, "Currently trading with another user, wait a little bit and try to trade with me.");
                    Log.Info("Already Trading, try again later");
                }
                return true;
            }
        }

        public override void OnLoginCompleted()
        {
        }

        public override void OnChatRoomMessage(SteamID chatID, SteamID sender, string message)
        {
            Log.Info(Bot.SteamFriends.GetFriendPersonaName(sender) + ": " + message);
            base.OnChatRoomMessage(chatID, sender, "Sorry, I'm a robot. No good at conversation :)  Open trade with me.");
        }

        public void listCurrentValues()
        {
            Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg, "Current trading values are: ");
            Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg, "My " + Bot.OTHER_A_TO_B + " " + ItemBDesc + "s for your 1 " + ItemADesc);
            Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg, "My 1 " + ItemADesc + " for your " + Bot.OTHER_B_TO_A + " " + ItemBDesc + "s");
        }

        public override void OnFriendRemove()
        {
        }

        public override void OnMessage(string message, EChatEntryType type)
        {
            if (inFriendsList())
            {
                Bot.SteamFriends.SendChatMessage(OtherSID, type, Bot.ChatResponse);
                listCurrentValues();
            }
            else
            {
                Bot.SteamFriends.SendChatMessage(OtherSID, type, "Please add me to friends if you would like to trade.");
            }
        }

        protected void pruneFriendsList()
        {
            // at this point, the client has received it's friends list
            int friendCount = Bot.SteamFriends.GetFriendCount();

            if(friendCount > friendsListMax)
            {
                //need to prune
                for (int x = 0; x < 20; x++)
                {
                    // steamids identify objects that exist on the steam network, such as friends, as an example
                    SteamID steamIdFriend = Bot.SteamFriends.GetFriendByIndex(x);
                    if (!Bot.Admins.Contains(steamIdFriend))
                    {
                        Bot.SteamFriends.RemoveFriend(steamIdFriend);
                        Bot.SteamFriends.IgnoreFriend(steamIdFriend, true);
                        Bot.SteamFriends.IgnoreFriend(steamIdFriend, false);
                    }
                }
            }
        }

        protected void removeFriendRequests()
        {
            // at this point, the client has received it's friends list
            int friendCount = Bot.SteamFriends.GetFriendCount();

            for (int x = 0; x < friendCount; x++)
            {
                // steamids identify objects that exist on the steam network, such as friends, as an example
                SteamID steamIdFriend = Bot.SteamFriends.GetFriendByIndex(x);
                if (!Bot.Admins.Contains(steamIdFriend.AccountID))
                {
                    EFriendRelationship rela = Bot.SteamFriends.GetFriendRelationship(steamIdFriend);
                    //String name = Bot.SteamFriends.GetFriendPersonaName(steamIdFriend);
                    if (rela == EFriendRelationship.RequestRecipient)
                    {
                        Bot.SteamFriends.RemoveFriend(steamIdFriend);
                        Bot.SteamFriends.IgnoreFriend(steamIdFriend, true);
                        Bot.SteamFriends.IgnoreFriend(steamIdFriend, false);
                    }
                }
            }
        }

        public override bool OnTradeRequest()
        {
            if (inFriendsList())
            {
                if (Bot.CurrentTrade == null)
                {
                    return true;
                }
                else
                {
                    Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg, "Currently trading with another user, wait a little bit and try again.");
                    Log.Info("Already Trading, try again later or try one of my other bots: http://steamcommunity.com/groups/deanBOT");
                    return false;
                }
            }
            else
            {
                Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg, "Please add me to friends if you would like to trade.");
                return false;
            }
        }

        public override void OnTradeError(string error)
        {
            Bot.SteamFriends.SendChatMessage(OtherSID,
                                              EChatEntryType.ChatMsg,
                                              "Oh, there was an error: " + error + "."
                                              );
            Bot.log.Warn(error);

            //prune check friends list
            pruneFriendsList();
        }

        public override void OnTradeTimeout()
        {
            Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg,
                                              "Sorry, but you were AFK and the trade was cancelled.");
            Bot.log.Info("User was kicked because he was AFK.");
        }

        public override void OnTradeInit()
        {
            //get inventory for finding rarity later
            myRawJsonInventory = Inventory.GetInventory(Bot.SteamUser.SteamID);

            OfferedItemsCount = 0;
            PossibleItemA = new Stack<Schema.Item>();
            PossibleItemB = new Stack<Schema.Item>();
            OfferedItems = new Stack<Schema.Item>();
            sendBothMessage("Readying some items for this trade...");
            sendBothMessage("Attempting to keep item list as unique as possible and reduce duplicates for you...");
            listCurrentValues();
            cacheItems();
        }

        private void cacheItems()
		{
			int bCached = PossibleItemB.ToArray().Length;
            int aCached = PossibleItemA.ToArray().Length;
            
			Schema.Item schemaItem;
            SteamTrade.Inventory.Item item;
			
			Stack<Schema.Item> DupeItemA = new Stack<Schema.Item>();
            Stack<Schema.Item> DupeItemB = new Stack<Schema.Item>();

            Stack<ushort> dupeCheckA = new Stack<ushort>();
            Stack<ushort> dupeCheckB = new Stack<ushort>();
			
            for (int i = Trade.MyInventory.Items.Length - 1; i > 0; i--)
            {
                item = Trade.MyInventory.Items[i];
                if (item.IsNotTradeable)
                {
                    //skip stuff we can't trade
                    continue;
                }

                if (whitelist.Contains(item.Defindex))
                {
                    //skip whitelist
                    Log.Info("Skipped item " + item.Defindex + " in whitelist.");
                    continue;
                }
                
				schemaItem = GetExtraMyItemInfo(item.Id);

                if (isQualifiedItemB(schemaItem))
                {
                    if (bCached < MY_MAX_B)
                    {
                        if (!uniqueOnlyB || !dupeCheckB.Contains(schemaItem.Defindex))
						{
							PossibleItemB.Push(schemaItem);
                            dupeCheckB.Push(schemaItem.Defindex);
							bCached++;
						}
						else
						{
							DupeItemB.Push(schemaItem);
						}
                    }
                }
                else if (isQualifiedItemA(schemaItem))
                {
                    if (aCached < MY_MAX_A)
                    {
						if(!uniqueOnlyA || !dupeCheckA.Contains(schemaItem.Defindex))
						{
							PossibleItemA.Push(schemaItem);
                            dupeCheckA.Push(schemaItem.Defindex);
							aCached++;
						}
						else
						{
							DupeItemA.Push(schemaItem);
						}
                    }
                }

                if (bCached >= MY_MAX_B && aCached >= MY_MAX_A)
                {
                    break;
                }

                schemaItem = null;
                item = null;
            }
			
            if(uniqueOnlyB)
            {
			    //add in dupes now
                for (int i = PossibleItemB.ToArray().Length; i < MY_MAX_B; i++)
			    {
                    if (DupeItemB.ToArray().Length == 0)
                    {
                        break;
                    }
                        PossibleItemB.Push(DupeItemB.Pop());
			    }

                //reverse stacks
                Schema.Item[] tempB = PossibleItemB.ToArray();
                Array.Reverse(tempB);
                PossibleItemB = new Stack<Schema.Item>(tempB);
			}

            if (uniqueOnlyA)
            {
                for (int j = PossibleItemA.ToArray().Length; j < MY_MAX_A; j++)
                {
                    if (DupeItemA.ToArray().Length == 0)
                    {
                        break;
                    }
                    PossibleItemA.Push(DupeItemA.Pop());
                }

                //reverse stacks
                Schema.Item[] tempA = PossibleItemA.ToArray();
                Array.Reverse(tempA);
                PossibleItemA = new Stack<Schema.Item>(tempA);
            }

            bCached = PossibleItemB.ToArray().Length;
            aCached = PossibleItemA.ToArray().Length;

            sendBothMessage("Completed. Readied " + bCached + " " + ItemBDesc + "s and " + aCached + " " + ItemADesc + "s for this trade.");
            Log.Info("Trade initiated with user: " + OtherSID.ToString() + ", cached " + bCached + " " + ItemBDesc + "s and " + aCached + " " + ItemADesc + "s for this trade.");
            sendBothMessage("Trade initiated. I accept " + ItemBDesc + "s or " + ItemADesc + "s, depending on which direction you want to trade.  See the DotA2Lounge trade for exchange rates.");
            sendBothMessage("Place your items and wait 5-15 seconds for me to process them.");
            

		}

        public override void OnTradeAddItem(Schema.Item schemaItem, Inventory.Item inventoryItem)
        {
            if (schemaItem != null)
            {
                Log.Info("User added item: " + schemaItem.ItemName);
                DecideMode();
                refreshOurTrade();
            }
            else
            {
                Trade.RemoveAllItems();
                Log.Info("Bot removed all items");
                sendBothMessage("Invalid item, must be a DotA 2 item.");
            }
        }

        private void DecideMode()
        {
            if (Trade.OtherOfferedItems.ToArray().Length > 0)
            {
                ulong[] otherItems = Trade.OtherOfferedItems.ToArray();
                ulong firstItemId = otherItems[0];
                Schema.Item schemaItem = GetExtraForeignItemInfo(firstItemId);
                if (isQualifiedItemA(schemaItem))
                {
                    if (Mode != TradeMode.OtherItemA)
                    {
                        resetTradeOffer();
                        sendBothMessage("I see you put up a " + ItemADesc + ", I will trade you " + OTHER_A_TO_B + " " + ItemBDesc + "s.");
                    }
                    Mode = TradeMode.OtherItemA;
                }
                else
                {
                    if (Mode != TradeMode.OtherItemB)
                    {
                        resetTradeOffer();
                        sendBothMessage("I see you put up an item, please place " + OTHER_B_TO_A + " " + ItemBDesc + "s for a " + ItemADesc + ".");
                    }
                    Mode = TradeMode.OtherItemB;

                }
            }
        }

        private void resetTradeOffer()
        {
            Trade.RemoveAllItems();
            Log.Info("Bot removed all items");
            OfferedItemsCount = 0;
            while (OfferedItems.Count > 0)
            {
                Schema.Item item = OfferedItems.Pop();
                if (isQualifiedItemB(item))
                {
                    PossibleItemB.Push(item);
                }
                else if (isQualifiedItemA(item))
                {
                    PossibleItemA.Push(item);
                }
            }
        }

        private void refreshOurTrade()
        {
            if (IsAdmin)
            {
                return;
            }

            if (Trade.OtherOfferedItems.ToArray().Length == 0)
            {
                resetTradeOffer();
                return;
            }

            if (Mode == TradeMode.OtherItemB)
            {
                int numItemBLeft = getNumA();
                if (numItemBLeft < 0)
                {
                    sendBothMessage("Item confirmed, add " + numItemBLeft * -1 + " more " + ItemBDesc + "s for a " + ItemADesc + "!");
                    return;
                }

                if (Trade.OtherOfferedItems.ToArray().Length > OTHER_MAX_B)
                {
                    sendBothMessage("Sorry, we will only accept up to " + OTHER_MAX_B + " " + ItemBDesc + "s");
                    return;
                }

                //put up item A
                if (ValidateItems())
                {
                    addRemoveA();
                }
                else
                {
                    sendBothMessage("Error: your trade is not valid.");
                }
            }
            else
            {
                if (Trade.OtherOfferedItems.ToArray().Length > OTHER_MAX_A)
                {
                    sendBothMessage("Sorry, we will only accept up to " + OTHER_MAX_A + " " + ItemADesc + "s");
                    return;
                }

                if (ValidateItems())
                {
                    addRemoveB();
                }
                else
                {
                    sendBothMessage("Error: your trade is not valid.");
                }
            }
        }

        private bool RemoveAllByItem(int defindex, uint numToRemove, string error)
        {
            if (Trade.RemoveAllItemsByDefindex(defindex, numToRemove) != numToRemove)
            {
                Bot.SteamFriends.SendChatMessage(OtherSID,
                                        EChatEntryType.ChatMsg,
                                        error
                                        );
                Trade.CancelTrade();

                Log.Error(error);
                Log.Info("Trade cancelled with user: " + OtherSID.ToString() + ", tried to remove all of item: " + defindex);
                return false;
            }
            return true;
        }

        private void addRemoveA()
        {
            int numItemA = getNumA();
            sendBothMessage("Verified, we will put up " + numItemA + " " + ItemADesc + "s");

            if (OfferedItemsCount > numItemA)
            {
                for (int i = OfferedItemsCount; i > numItemA; i--)
                {
                    //remove item A
                    Schema.Item item = OfferedItems.Pop();
                    PossibleItemA.Push(item);

                    //add x Item A until correct
                    if (!Trade.RemoveItemByDefindex(item.Defindex))
                    {
                        sendBothMessage("Oh, there was an error add/removing " + ItemADesc + "s! Try again later.");
                        Log.Error("Error adding item: " + item.ItemName);
                        Log.Error("Oh, there was an error add/removing " + ItemADesc + "s! Try again later.");
                        resetTradeOffer();
                        break;
                    }
                    Log.Info("Bot remove item: " + item.ItemName);
                    OfferedItemsCount--;
                }
            }

            if (OfferedItemsCount < numItemA)
            {
                for (int i = OfferedItemsCount; i < numItemA; i++)
                {
                    if (PossibleItemA.ToArray().Length == 0)
                    {
                        sendBothMessage("Sorry, we ran out of " + ItemADesc + "s! Would you like to exchange your " + ItemADesc + "s for " + ItemBDesc + "s instead?");                        
                        Log.Error("User tried to get some " + ItemADesc + "s but ran out! Try again later.");
                        resetTradeOffer();
                        break;
                    }

                    Schema.Item item = PossibleItemA.Pop();
                    OfferedItems.Push(item);

                    //add x item A until correct
                    if (!Trade.AddItemByDefindex(item.Defindex))
                    {
                        sendBothMessage("Sorry, we ran out of " + ItemADesc + "s! Would you like to exchange your " + ItemADesc + "s for " + ItemBDesc + "s instead?");
                        Log.Error("Error adding item: " + item.ItemName);
                        Log.Error("Oh, there was an error add/removing " + ItemADesc + "s! Try again later.");
                        resetTradeOffer();
                        break;
                    }
                    Log.Info("Bot added item: " + item.ItemName);
                    OfferedItemsCount++;
                }
            }
            else
            {
                //do nothing
            }
        }

        private int getNumB()
        {
            int offeredItems = Trade.OtherOfferedItems.ToArray().Length;

            if (offeredItems > OTHER_MAX_A)
            {
                offeredItems = OTHER_MAX_A;
            }

            int numItemB = offeredItems * OTHER_A_TO_B;
            //bonus
            if (Trade.OtherOfferedItems.ToArray().Length == 3)
            {
                //make 16 for 3x
                numItemB = Trade.OtherOfferedItems.ToArray().Length * OTHER_A_TO_B + 1;
            }
            return numItemB;
        }

        private int getNumA()
        {
            int offeredItems = Trade.OtherOfferedItems.ToArray().Length;

            if (offeredItems > OTHER_MAX_B)
            {
                offeredItems = OTHER_MAX_B;
            }

            //bonus
            if (offeredItems > OTHER_B_TO_A * 2)
            {
                offeredItems = offeredItems + 1;
            }

            if (offeredItems % OTHER_B_TO_A != 0)
            {
                return offeredItems % OTHER_B_TO_A - OTHER_B_TO_A;
            }

            int numA = offeredItems / OTHER_B_TO_A;

            return numA;
        }

        private void addRemoveB()
        {
            int numItemB = getNumB();

            sendBothMessage("Verified, we would put up " + numItemB + " " + ItemBDesc + "s");

            if (OfferedItemsCount > numItemB)
            {
                for (int i = OfferedItemsCount; i > numItemB; i--)
                {
                    //remove Item B
                    Schema.Item item = OfferedItems.Pop();
                    PossibleItemB.Push(item);

                    //add x Item B until correct
                    if (!Trade.RemoveItemByDefindex(item.Defindex))
                    {
                        sendBothMessage("Oh, there was an error add/removing " + ItemBDesc + "s! Try again later.");
                        Log.Error("Error adding item: " + item.ItemName);
                        Log.Error("Oh, there was an error add/removing " + ItemBDesc + "s! Try again later.");
                        resetTradeOffer();
                        break;
                    }
                    Log.Info("Bot remove item: " + item.ItemName);
                    OfferedItemsCount--;
                }
            }

            if (OfferedItemsCount < numItemB)
            {
                for (int i = OfferedItemsCount; i < numItemB; i++)
                {
                    if (PossibleItemB.ToArray().Length == 0)
                    {
                        sendBothMessage("Sorry, we ran out of " + ItemBDesc + "s! Would you like to exchange your " + ItemBDesc + "s for " + ItemADesc + "s instead?");
                        Log.Error("User tried to get some " + ItemBDesc + "s but ran out! Try again later.");
                        resetTradeOffer();
                        break;
                    }

                    Schema.Item item = PossibleItemB.Pop();
                    OfferedItems.Push(item);

                    //add x Item A until correct
                    if (!Trade.AddItemByDefindex(item.Defindex))
                    {
                        sendBothMessage("Sorry, we ran out of " + ItemBDesc + "s! Would you like to exchange your " + ItemBDesc + "s for " + ItemADesc + "s instead?");
                        Log.Error("Error adding item: " + item.ItemName);
                        Log.Error("Oh, there was an error add/removing " + ItemBDesc + "s! Try again later.");
                        resetTradeOffer();
                        break;
                    }
                    Log.Info("Bot added item: " + item.ItemName);
                    OfferedItemsCount++;
                }
            }
            else
            {
                //do nothing
            }
        }

        public override void OnTradeRemoveItem(Schema.Item schemaItem, Inventory.Item inventoryItem)
        {
            if (schemaItem != null)
            {
                Log.Info("User removed item: " + schemaItem.ItemName);
                DecideMode();
                refreshOurTrade();
            }
        }

        public override void OnTradeMessage(string message)
        {
            if (IsAdmin)
            {
                sendBothMessage("Admin Message: " + message);

                handleAdmin(message);
            }
            else
            {
                sendBothMessage("Sorry, I'm a robot. No good at conversation :)  Place your " + ItemADesc + "s or " + ItemBDesc + "s and I will automatically place the equivalent when you place enough.");
            }


        }

        public override void OnTradeReady(bool ready)
        {
            //Because SetReady must use its own version, it's important
            //we poll the trade to make sure everything is up-to-date.
            Trade.Poll();

            if (!ready)
            {
                Trade.SetReady(false);
            }
            else
            {
                if (IsAdmin || (ValidateItems() && ValidateFinal()))
                {
                    Trade.SetReady(true);
                }
                //Trade.SendMessage ("Scrap: " + ScrapPutUp);
            }
        }

        public override void OnTradeAccept()
        {
            if (IsAdmin || (ValidateItems() && ValidateFinal()))
            {
                //Even if it is successful, AcceptTrade can fail on
                //trades with a lot of items so we use a try-catch
                try
                {
                    Trade.AcceptTrade();
                }
                catch
                {
                    Log.Warn("The trade might have failed, but we can't be sure.");
                }

                Log.Success("Trade Complete!");
            }

            OnTradeClose();
            Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg,
                                              "Thank you for trading.  Please check out my other bots at http://steamcommunity.com/groups/deanBOT");
        }

        public bool ValidateItems()
        {
            if (Mode == TradeMode.OtherItemB)
            {
                foreach (ulong i in Trade.OtherOfferedItems)
                {
                    Inventory.Item item = Trade.OtherInventory.GetItem(i);
                    Schema.Item schemaItem = GetExtraForeignItemInfo(i);

                    if (!validateB(schemaItem, item))
                    {
                        return false;
                    }
                }
                return true;
                
            }
            else
            {
                foreach (ulong i in Trade.OtherOfferedItems)
                {
                    Schema.Item schemaItem = GetExtraForeignItemInfo(i);
                    
                    if (!validateA(schemaItem))
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        protected virtual bool validateA(Schema.Item schemaItem)
        {
            if (!isQualifiedItemA(schemaItem))
            {
                sendBothMessage("Sorry, item you offered: " + schemaItem.ItemName + " is not a " + ItemADesc + ".");
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

        protected virtual bool validateB(Schema.Item schemaItem, Inventory.Item item)
        {
            if (!isQualifiedItemB(schemaItem))
            {
                sendBothMessage("Sorry, item you offered: " + schemaItem.ItemName + " is not a " + ItemBDesc + ".");
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

        public bool ValidateFinal()
        {
            if (Trade.OtherOfferedItems == null)
            {
                Bot.SteamFriends.SendChatMessage(OtherSID,
                                                EChatEntryType.ChatMsg,
                                                "Oh, there was an error: Steam issue. Try again later"
                                                );
                Log.Error("Oh, there was an error: Steam issue. Try again later");
                Trade.CancelTrade();

                return false;
            }
            int otherOfferedCount = Trade.OtherOfferedItems.ToArray().Length;
            if (Mode == TradeMode.OtherItemB)
            {
                if (getNumA() != OfferedItemsCount)
                {
                    sendBothMessage("Desync on the trade, offer isn't valid.");
                    return false;
                }
                else if (otherOfferedCount > OTHER_MAX_B)
                {
                    sendBothMessage("Sorry, you cannot offer more than " + OTHER_MAX_B + " " + ItemBDesc + "s.");
                    return false;
                }

                Log.Success("Trading " + OfferedItemsCount + " " + ItemADesc + "s for " + otherOfferedCount + " " + ItemBDesc + "s.");
                return true;
            }
            else
            {
                if (getNumB() != OfferedItemsCount)
                {
                    sendBothMessage("Desync on the trade, offer isn't valid.");
                    return false;
                }
                else if (otherOfferedCount > OTHER_MAX_A)
                {
                    sendBothMessage("Sorry, you cannot offer more than " + OTHER_MAX_A + " " + ItemADesc + "s.");
                    return false;
                }

                Log.Info("Trading " + OfferedItemsCount + " " + ItemBDesc + "s for " + otherOfferedCount + " " + ItemADesc + "s.");
                return true;
            }
        }

        /*
         * goes through the schema and gets extra item info 
         */
        private Schema.Item GetExtraForeignItemInfo(ulong itemid)
        {
            Inventory.Item item = Trade.OtherInventory.GetItem(itemid);
            Schema.Item schemaItem = Trade.CurrentSchema.GetItem(item.Defindex);

            //get extra attributes from private inventory because they're not in the schema
            schemaItem.Rarity = Trade.GetItemRarityFromPrivateBp(2, itemid);
            schemaItem.TrueName = Trade.GetItemTrueNameFromPrivateBp(2, itemid);
            return schemaItem;
        }

        /*
         * goes through the schema and gets extra item info 
         */
        private Schema.Item GetExtraMyItemInfo(ulong itemId)
        {
            Inventory.Item item = Trade.MyInventory.GetItem(itemId);
            Schema.Item schemaItem = Trade.CurrentSchema.GetItem(item.Defindex);

            //get extra attributes from private inventory because they're not in the schema
            uint classId = GetClassIdForItemId(itemId);
            ulong iid = GetInstanceIdForItemId(itemId);

            string index = classId + "_" + iid;

            string rarity = "";
            string truename = "";

            try
            {
                // for tf2 the def index is in the app_data section in the 
                // descriptions object. this may not be the case for all
                // games and therefore this may be non-portable.
                rarity = myRawJsonInventory.rgDescriptions[index].tags[1].name;
                truename = myRawJsonInventory.rgDescriptions[index].tags[0].name;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            schemaItem.Rarity = rarity;
            schemaItem.TrueName = truename;
            return schemaItem;
        }

        /// <summary>
        /// Gets the class id for the given item.
        /// </summary>
        /// <param name="itemId">The item id.</param>
        /// <returns>A class ID or 0 if there is an error.</returns>
        public uint GetClassIdForItemId(ulong itemId)
        {
            string i = itemId.ToString(CultureInfo.InvariantCulture);

            try
            {
                return myRawJsonInventory.rgInventory[i].classid;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return 0;
            }
        }

        /// <summary>
        /// Gets the instance id for given item.
        /// </summary>
        /// <param name="itemId">The item id.</param>
        /// <returns>A instance ID or 0 if there is an error.</returns>
        public ulong GetInstanceIdForItemId(ulong itemId)
        {
            string i = itemId.ToString(CultureInfo.InvariantCulture);

            try
            {
                return myRawJsonInventory.rgInventory[i].instanceid;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return 0;
            }
        }

        /*
         * check if user is in friends list
         */
        private bool inFriendsList()
        {
            // at this point, the client has received it's friends list
            int friendCount = Bot.SteamFriends.GetFriendCount();

            for (int x = 0; x < friendCount; x++)
            {
                // steamids identify objects that exist on the steam network, such as friends, as an example
                SteamID steamIdFriend = Bot.SteamFriends.GetFriendByIndex(x);
                if (steamIdFriend.Equals(OtherSID))
                {
                    return true;
                }
            }
            return false;
        }

        /*
         * If we have invalid items, turn off bot immediately
         */
        private void checkForInvalidItems()
        {
            foreach (SteamTrade.Inventory.Item item in Trade.MyInventory.Items)
            {
                if (item.IsNotTradeable || item.Defindex == 15003)
                {
                    //skip stuff we can't trade and keys
                    continue;
                }

                //check rarity of everything else
                Schema.Item schemaItem = GetExtraMyItemInfo(item.Id);

                if (schemaItem.Rarity == "Common")
                {
                    Log.Info("Stopping bot, found an invalid item in inventory: " + schemaItem.ItemName);
                    Bot.StopBot();
                    return;
                }
            }
        }

        private void handleAdmin(string command)
        {
            Log.Info("ADMIN " + OtherSID.ToString() + " gave command: " + command);
            char[] separators = { ':' };
            string[] tokenizedCommand = command.Split(separators);
            
            SteamTrade.Inventory.Item item;
            Schema.Item schemaItem;

            if (tokenizedCommand.Length >= 2)
            {
                string type = tokenizedCommand[0].ToLower();
                switch (type)
                {
                    case "give":
                        string itemindex = tokenizedCommand[1];
                        int defindex = Convert.ToInt32(itemindex);
                        if (Trade.MyInventory.GetItemsByDefindex(defindex).ToArray().Length == 0)
                        {
                            sendBothMessage("We don't have item: " + defindex + ", you sure that's right Joe?");
                        }
                        else if (!Trade.AddItemByDefindex(defindex))
                        {
                            Bot.SteamFriends.SendChatMessage(OtherSID,
                                                  EChatEntryType.ChatMsg,
                                                  "Oh, there was an error: couldn't add item."
                                                  );

                            Log.Error("Oh, there was an error: couldn't add item: " + defindex);
                            break;
                        }
                        Log.Info("Fulfilled command: " + command);
                        break;
                    case "name":
                        string searchString = tokenizedCommand[1].ToLower();
			
                        for (int i = Trade.MyInventory.Items.Length - 1; i > 0; i--)
                        {
                            item = Trade.MyInventory.Items[i];
                            if (item.IsNotTradeable)
                            {
                                //skip stuff we can't trade
                                continue;
                            }
                
				            schemaItem = GetExtraMyItemInfo(item.Id);

                            if(schemaItem.Name.ToLower().Contains(searchString))
                            {
                                if (!Trade.AddItemByDefindex(schemaItem.Defindex))
                                {
                                    Bot.SteamFriends.SendChatMessage(OtherSID,
                                                          EChatEntryType.ChatMsg,
                                                          "Oh, there was an error: couldn't add item."
                                                          );

                                    Log.Error("Oh, there was an error: couldn't add item: " + item.Defindex);
                                    break;
                                }
                                Log.Info("Fulfilled command: " + command);
                                break;
                            }
                        }
                        break;
                    case "rarity":
                        string rarity = tokenizedCommand[1].ToLower();

                        int max = 999;

                        if (tokenizedCommand.Length >= 3)
                        {
                            max = Convert.ToInt16(tokenizedCommand[2]);

                        }
                        int count = 0;
                        for (int i = Trade.MyInventory.Items.Length - 1; i > 0; i--)
                        {
                            item = Trade.MyInventory.Items[i];
                            if (item.IsNotTradeable)
                            {
                                //skip stuff we can't trade
                                continue;
                            }

                            schemaItem = GetExtraMyItemInfo(item.Id);

                            if (schemaItem.Rarity.ToLower().Equals(rarity))
                            {
                                if (!Trade.AddItemByDefindex(schemaItem.Defindex))
                                {
                                    Bot.SteamFriends.SendChatMessage(OtherSID,
                                                          EChatEntryType.ChatMsg,
                                                          "Oh, there was an error: couldn't add item."
                                                          );

                                    Log.Error("Oh, there was an error: couldn't add item: " + item.Defindex);
                                    break;
                                }
                                Log.Info("Fulfilled command: " + command);
                                count++;
                            }
                            if (count >= max)
                            {
                                break;
                            }
                        }
                        break;
                    case "truename":
                        string truename = tokenizedCommand[1].ToLower();

                        int maxb = 999;

                        if (tokenizedCommand.Length >= 3)
                        {
                            maxb = Convert.ToInt16(tokenizedCommand[2]);

                        }
                        int countb = 0;
                        for (int i = Trade.MyInventory.Items.Length - 1; i > 0; i--)
                        {
                            item = Trade.MyInventory.Items[i];
                            if (item.IsNotTradeable)
                            {
                                //skip stuff we can't trade
                                continue;
                            }

                            schemaItem = GetExtraMyItemInfo(item.Id);

                            if (schemaItem.TrueName.ToLower().Equals(truename))
                            {
                                if (!Trade.AddItemByDefindex(schemaItem.Defindex))
                                {
                                    Bot.SteamFriends.SendChatMessage(OtherSID,
                                                          EChatEntryType.ChatMsg,
                                                          "Oh, there was an error: couldn't add item."
                                                          );

                                    Log.Error("Oh, there was an error: couldn't add item: " + item.Defindex);
                                    break;
                                }
                                Log.Info("Fulfilled command: " + command);
                                countb++;
                            }
                            if (countb >= maxb)
                            {
                                break;
                            }
                        }
                        break;
                    default:
                        break;

                }
            }
        }

    }

}

