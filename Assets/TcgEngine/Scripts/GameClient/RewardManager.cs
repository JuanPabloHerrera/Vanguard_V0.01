using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace TcgEngine.Client
{
    //Grants rewards for adventure and solo mode

    public class RewardManager : MonoBehaviour
    {
        private bool reward_gained = false;

        private static RewardManager instance;

        void Awake()
        {
            instance = this;
        }

        private void Start()
        {
            GameClient.Get().onGameEnd += OnGameEnd;
        }

        void OnGameEnd(int winner)
        {
            int player_id = GameClient.Get().GetPlayerID();
            
            // Adventure mode rewards
            if (GameClient.game_settings.game_type == GameType.Adventure && winner == player_id)
            {
                UserData udata = Authenticator.Get().UserData;
                LevelData level = LevelData.Get(GameClient.game_settings.level);
                if (level != null && !udata.HasReward(level.id) && !reward_gained)
                {
                    if (Authenticator.Get().IsTest())
                        GainRewardTest(level);
                    if (Authenticator.Get().IsApi())
                        GainRewardAPI(level);
                }
            }
            
            // Solo mode rewards
            if (GameClient.game_settings.game_type == GameType.Solo && !reward_gained)
            {
                int coins = 0;
                if (winner == player_id)
                {
                    coins = 100; // Win
                }
                else if (winner == -1)
                {
                    coins = 50; // Tie
                }
                else
                {
                    coins = 25; // Loss
                }
                
                if (Authenticator.Get().IsTest())
                    GainSoloRewardTest(coins);
                if (Authenticator.Get().IsApi())
                    GainSoloRewardAPI(coins);
            }
        }

        private async void GainRewardTest(LevelData level)
        {
            VariantData variant = VariantData.GetDefault();
            UserData udata = Authenticator.Get().UserData;
            udata.coins += level.reward_coins;
            udata.xp += level.reward_xp;
            udata.AddReward(level.id);

            foreach (CardData card in level.reward_cards)
            {
                udata.AddCard(card.id, variant.id, 1);
            }

            foreach (PackData pack in level.reward_packs)
            {
                udata.AddPack(pack.id, 1);
            }

            reward_gained = true;
            await Authenticator.Get().SaveUserData();
        }

        private async void GainRewardAPI(LevelData level)
        {
            bool success = await GainRewardAPI(level.id);
            reward_gained = success;
        }

        public async Task<bool> GainRewardAPI(string reward_id)
        {
            RewardGainRequest req = new RewardGainRequest();
            req.reward = reward_id;

            string url = ApiClient.ServerURL + "/users/rewards/gain/" + ApiClient.Get().UserID;
            string json = ApiTool.ToJson(req);
            WebResponse res = await ApiClient.Get().SendPostRequest(url, json);
            Debug.Log("Gain Reward: " + reward_id + " " + res.success);
            return res.success;
        }

        private async void GainSoloRewardTest(int coins)
        {
            UserData udata = Authenticator.Get().UserData;
            udata.coins += coins;
            reward_gained = true;
            await Authenticator.Get().SaveUserData();
            Debug.Log("Solo Mode Reward: " + coins + " coins");
        }

        private async void GainSoloRewardAPI(int coins)
        {
            // For API mode, we might need to send a request to the server
            // For now, we'll use the test method as a fallback
            UserData udata = Authenticator.Get().UserData;
            udata.coins += coins;
            reward_gained = true;
            await Authenticator.Get().SaveUserData();
            Debug.Log("Solo Mode Reward: " + coins + " coins");
        }

        public bool IsRewardGained()
        {
            return reward_gained;
        }

        public static RewardManager Get()
        {
            return instance;
        }
    }
}
