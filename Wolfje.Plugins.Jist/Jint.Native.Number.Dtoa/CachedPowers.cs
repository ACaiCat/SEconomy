#define DEBUG
using System;
using System.Diagnostics;

namespace Jint.Native.Number.Dtoa
{
	public class CachedPowers
	{
		private class CachedPower
		{
			internal readonly long Significand;

			internal readonly short BinaryExponent;

			internal readonly short DecimalExponent;

			internal CachedPower(ulong significand, short binaryExponent, short decimalExponent)
			{
				Significand = (long)significand;
				BinaryExponent = binaryExponent;
				DecimalExponent = decimalExponent;
			}
		}

		private const double Kd1Log210 = 0.30102999566398114;

		private const int CachedPowersSpacing = 8;

		private static readonly CachedPower[] CACHED_POWERS = new CachedPower[82]
		{
			new CachedPower(16580792590934885855uL, -1087, -308),
			new CachedPower(12353653155963782858uL, -1060, -300),
			new CachedPower(18408377700990114895uL, -1034, -292),
			new CachedPower(13715310171984221708uL, -1007, -284),
			new CachedPower(10218702384817765436uL, -980, -276),
			new CachedPower(15227053142812498563uL, -954, -268),
			new CachedPower(11345038669416679861uL, -927, -260),
			new CachedPower(16905424996341287883uL, -901, -252),
			new CachedPower(12595523146049147757uL, -874, -244),
			new CachedPower(9384396036005875287uL, -847, -236),
			new CachedPower(13983839803942852151uL, -821, -228),
			new CachedPower(10418772551374772303uL, -794, -220),
			new CachedPower(15525180923007089351uL, -768, -212),
			new CachedPower(11567161174868858868uL, -741, -204),
			new CachedPower(17236413322193710309uL, -715, -196),
			new CachedPower(12842128665889583758uL, -688, -188),
			new CachedPower(9568131466127621947uL, -661, -180),
			new CachedPower(14257626930069360058uL, -635, -172),
			new CachedPower(10622759856335341974uL, -608, -164),
			new CachedPower(15829145694278690180uL, -582, -156),
			new CachedPower(11793632577567316726uL, -555, -148),
			new CachedPower(17573882009934360870uL, -529, -140),
			new CachedPower(13093562431584567480uL, -502, -132),
			new CachedPower(9755464219737475723uL, -475, -124),
			new CachedPower(14536774485912137811uL, -449, -116),
			new CachedPower(10830740992659433045uL, -422, -108),
			new CachedPower(16139061738043178685uL, -396, -100),
			new CachedPower(12024538023802026127uL, -369, -92),
			new CachedPower(17917957937422433684uL, -343, -84),
			new CachedPower(13349918974505688015uL, -316, -76),
			new CachedPower(9946464728195732843uL, -289, -68),
			new CachedPower(14821387422376473014uL, -263, -60),
			new CachedPower(11042794154864902060uL, -236, -52),
			new CachedPower(16455045573212060422uL, -210, -44),
			new CachedPower(12259964326927110867uL, -183, -36),
			new CachedPower(18268770466636286478uL, -157, -28),
			new CachedPower(13611294676837538539uL, -130, -20),
			new CachedPower(10141204801825835212uL, -103, -12),
			new CachedPower(15111572745182864684uL, -77, -4),
			new CachedPower(11258999068426240000uL, -50, 4),
			new CachedPower(16777216000000000000uL, -24, 12),
			new CachedPower(12500000000000000000uL, 3, 20),
			new CachedPower(9313225746154785156uL, 30, 28),
			new CachedPower(13877787807814456755uL, 56, 36),
			new CachedPower(10339757656912845936uL, 83, 44),
			new CachedPower(15407439555097886824uL, 109, 52),
			new CachedPower(11479437019748901445uL, 136, 60),
			new CachedPower(17105694144590052135uL, 162, 68),
			new CachedPower(12744735289059618216uL, 189, 76),
			new CachedPower(9495567745759798747uL, 216, 84),
			new CachedPower(14149498560666738074uL, 242, 92),
			new CachedPower(10542197943230523224uL, 269, 100),
			new CachedPower(15709099088952724970uL, 295, 108),
			new CachedPower(11704190886730495818uL, 322, 116),
			new CachedPower(17440603504673385349uL, 348, 124),
			new CachedPower(12994262207056124023uL, 375, 132),
			new CachedPower(9681479787123295682uL, 402, 140),
			new CachedPower(14426529090290212157uL, 428, 148),
			new CachedPower(10748601772107342003uL, 455, 156),
			new CachedPower(16016664761464807395uL, 481, 164),
			new CachedPower(11933345169920330789uL, 508, 172),
			new CachedPower(17782069995880619868uL, 534, 180),
			new CachedPower(13248674568444952270uL, 561, 188),
			new CachedPower(9871031767461413346uL, 588, 196),
			new CachedPower(14708983551653345445uL, 614, 204),
			new CachedPower(10959046745042015199uL, 641, 212),
			new CachedPower(16330252207878254650uL, 667, 220),
			new CachedPower(12166986024289022870uL, 694, 228),
			new CachedPower(18130221999122236476uL, 720, 236),
			new CachedPower(13508068024458167312uL, 747, 244),
			new CachedPower(10064294952495520794uL, 774, 252),
			new CachedPower(14996968138956309548uL, 800, 260),
			new CachedPower(11173611982879273257uL, 827, 268),
			new CachedPower(16649979327439178909uL, 853, 276),
			new CachedPower(12405201291620119593uL, 880, 284),
			new CachedPower(9242595204427927429uL, 907, 292),
			new CachedPower(13772540099066387757uL, 933, 300),
			new CachedPower(10261342003245940623uL, 960, 308),
			new CachedPower(15290591125556738113uL, 986, 316),
			new CachedPower(11392378155556871081uL, 1013, 324),
			new CachedPower(16975966327722178521uL, 1039, 332),
			new CachedPower(12648080533535911531uL, 1066, 340)
		};

		private const int GrisuCacheOffset = 308;

		internal static int GetCachedPower(int e, int alpha, int gamma, DiyFp cMk)
		{
			double num = System.Math.Ceiling((double)(alpha - e + 64 - 1) * 0.30102999566398114);
			int num2 = (308 + (int)num - 1) / 8 + 1;
			CachedPower cachedPower = CACHED_POWERS[num2];
			cMk.F = cachedPower.Significand;
			cMk.E = cachedPower.BinaryExponent;
			Debug.Assert(alpha <= cMk.E + e && cMk.E + e <= gamma);
			return cachedPower.DecimalExponent;
		}
	}
}
