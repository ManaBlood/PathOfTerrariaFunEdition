using PathOfTerraria.Common.Systems.PassiveTreeSystem;
using Terraria.Audio;
using Terraria.Localization;
using Terraria.ModLoader.IO;

namespace PathOfTerraria.Common.Systems.ModPlayers;

// ReSharper disable once ClassNeverInstantiated.Global
public class ExpModPlayer : ModPlayer
{
	public int Level;
	public int QuestLevel;
	public int EffectiveLevel => Level + QuestLevel;

	// Patch: Changed to long.
	public long Exp;

	// The two comments here stop the level cap - the "Level == 100" here, and the "Level >= 100" below.
	// I've also changed Exp into a long. This adds a substantial length to the level cap...kinda.
	// A 64 bit integer exp value can handle up to a whopping level ~278 before overflowing anyway.
	// A decimal may be superior, but I don't have the time to test it.
	// You can look into C#'s BigInteger, but that'd require more sweeping changes I don't have the time to make.
	// For most cases, slapping this file in place of the file of the same name in PathOfTerraria/Common/Systems/Players/ExpModPlayer.cs
	// SHOULD suffice. Otherwise, porting these two should be as easy as copy-pasting the LoadData modifications,
	// and commenting out the cap here.
	//
	// Note: ulong, an unsigned 64 bit integer, is slightly longer - but I don't think the TagCompound system
	// used to save/load data supports it, and it'd only go up to level ~283 anyway.
	// Also not that you may need to adjust the Math calls here, which use doubles as their return type.
	//
	// Patch: Changed to long, cast value.
	public long NextLevel => /*Level == 100 ? 1 : */(long)(Level * 250 + Math.Max(0, 80 * Math.Pow(2, 1 + Level * 0.2f)));

	public override void PreUpdate()
	{
		// Patch: Removed cap.
		if (Exp <= NextLevel)// || Level >= 100)
		{
			return;
		}

		Exp -= NextLevel;
		Level++;

		if (Main.myPlayer == Player.whoAmI && !Main.dedServ) //Only use level up text and sounds on the local client, despite progress being otherwise synced
		{
			SoundEngine.PlaySound(new SoundStyle($"{PoTMod.ModName}/Assets/Sounds/Tier5"));

			Main.NewText(Language.GetText("Mods.PathOfTerraria.Misc.Experience.LevelUp").WithFormatArgs(Level).Value, new Color(145, 255, 160));
			Main.NewText(Language.GetText("Mods.PathOfTerraria.Misc.Experience.SkillUp"), new Color(255, 255, 160));
		}

		Player.GetModPlayer<PassiveTreePlayer>().Points++;
	}

	public override void SaveData(TagCompound tag)
	{
		tag["level"] = Level;
		tag["questLevel"] = QuestLevel;
		tag["exp"] = Exp;
	}

	public override void LoadData(TagCompound tag)
	{
		Level = tag.GetInt("level");
		QuestLevel = tag.GetInt("questLevel");

		// Patch: Edited to check if it contains the tag, and then pattern matched to get the value instead using GetInt/Long. 
		if (tag.ContainsKey("exp"))
		{
			// Note this saving needs to be written safely. If you port an existing PoT character, using tag.GetInt/Long would crash,
			// because polymorphism. If you switch to BigInteger, you should be able to store as a string in the same format without issues.
			object exp = tag["exp"];

			if (exp is int intExp)
			{
				Exp = intExp;
			}
			else if (exp is long longExp)
			{
				Exp = longExp;
			}
		}
	}