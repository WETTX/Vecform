using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu]
public class SpikeRuleTile : RuleTile<SpikeRuleTile.Neighbor>
{
    // Ссылка на тайл земли, который будем искать
    public TileBase voidTile;

    // Новые типы соседей
    public class Neighbor : RuleTile.TilingRule.Neighbor
    {
        public const int VoidAbove = 3;  // Земля сверху
        public const int VoidRight = 4;  // Земля справа

        public const int VoidBelow = 5;  // Земля снизу
        public const int VoidLeft = 6;   // Земля слева
    }

    // Логика проверки соседей
    public override bool RuleMatch(int neighbor, TileBase tile)
    {
        switch (neighbor)
        {
            case Neighbor.VoidAbove:
                // Проверяем, является ли тайл нашей землёй
                return tile == voidTile;
            case Neighbor.VoidBelow:
                return tile == voidTile;
            case Neighbor.VoidLeft:
                return tile == voidTile;
            case Neighbor.VoidRight:
                return tile == voidTile;
        }
        return base.RuleMatch(neighbor, tile);
    }
}