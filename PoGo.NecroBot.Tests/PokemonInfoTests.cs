using PoGo.NecroBot.Logic.PoGoUtils;
using POGOProtos.Data;
using POGOProtos.Enums;
using Xunit;

namespace PoGo.NecroBot.Tests
{
    public class PokemonInfoTests
    {
        // When the CP multiplier is 0, perfection collapses to the simple IV average:
        // (2*atk + def + sta) / 60 * 100. These are exact and deterministic.
        [Theory]
        [InlineData(15, 15, 15, 100.0)] // perfect
        [InlineData(0, 0, 0, 0.0)]      // worst
        [InlineData(15, 0, 0, 50.0)]    // 30/60
        public void Perfection_WithZeroCpMultiplier_IsIvAverage(int atk, int def, int sta, double expected)
        {
            var poke = new PokemonData
            {
                PokemonId = PokemonId.Pidgey,
                CpMultiplier = 0f,
                AdditionalCpMultiplier = 0f,
                IndividualAttack = atk,
                IndividualDefense = def,
                IndividualStamina = sta
            };

            Assert.Equal(expected, PokemonInfo.CalculatePokemonPerfection(poke), 3);
        }

        [Fact]
        public void GetBaseStats_ReturnsKnownGen1Values()
        {
            // case 16 (Pidgey): new BaseStats(baseStamina:80, baseAttack:94, baseDefense:90)
            var stats = PokemonInfo.GetBaseStats(PokemonId.Pidgey);
            Assert.Equal(94, stats.BaseAttack);
            Assert.Equal(90, stats.BaseDefense);
            Assert.Equal(80, stats.BaseStamina);
        }

        [Fact]
        public void GetLevel_MapsCpMultiplierToLevel()
        {
            var poke = new PokemonData {CpMultiplier = 0.094f, AdditionalCpMultiplier = 0f};
            Assert.Equal(1, PokemonInfo.GetLevel(poke));
        }

        [Fact]
        public void MaxCp_IsNeverLessThanCurrentCp()
        {
            var poke = new PokemonData
            {
                PokemonId = PokemonId.Charmander,
                CpMultiplier = 0.5f,
                IndividualAttack = 10,
                IndividualDefense = 11,
                IndividualStamina = 12
            };

            Assert.True(PokemonInfo.CalculateMaxCp(poke) >= PokemonInfo.CalculateCp(poke));
        }
    }
}
