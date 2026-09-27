using System;

namespace ShiftingMetropolis.Battle
{
    /// <summary>
    /// 1つの技(コマンド)の定義。ためる/固有技/自由技/ブレイン・スマッシュ全てをこの型で表す。
    /// </summary>
    [Serializable]
    public class Skill
    {
        public string skillName;
        public SkillCommandType type;

        /// <summary>必要ゲージ量(ためる・ブレイン・スマッシュは0)。</summary>
        public int requiredGauge;

        /// <summary>基礎ダメージ(攻撃力-防御力)への倍率。</summary>
        public float powerMultiplier = 1.0f;

        /// <summary>命中率(0~1)。</summary>
        public float accuracy = 0.9f;

        /// <summary>必殺技演出(カットイン+スロー)の対象かどうか。プロトタイプでは演出未実装だがフラグのみ保持。</summary>
        public int requiredGear = 1;
        public bool isUltimate;
        public string ultimateId;

        public Skill(string skillName, SkillCommandType type, int requiredGauge, float powerMultiplier, float accuracy, bool isUltimate = false)
        {
            this.skillName = skillName;
            this.type = type;
            this.requiredGauge = requiredGauge;
            this.powerMultiplier = powerMultiplier;
            this.accuracy = accuracy;
            this.isUltimate = isUltimate;
        }
    }
}
