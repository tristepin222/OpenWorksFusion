﻿using System;
using System.Collections.Generic;
using ModIO.Implementation.API.Objects;
using ModIO.Implementation.API.Requests;
using ModIO.Implementation.Wss.Messages;
using ModIO.Implementation.Wss.Messages.Objects;
using Newtonsoft.Json.Utilities;
using UnityEngine;

namespace ModIO.Implementation.API
{
	/// <summary>
	/// You do not need to use this class. This is used to ensure specific types in API request
	/// objects and anything we serialize for the registry gets AOT code generated when using IL2CPP
	/// compilation.
	/// </summary>
	internal class AotTypeEnforcer : MonoBehaviour
	{
		public void Awake()
		{
			AotHelper.EnsureList<ModId>();
			AotHelper.EnsureList<ModObject>();
			AotHelper.EnsureList<LogoObject>();
			AotHelper.EnsureList<UserObject>();
			AotHelper.EnsureList<ImageObject>();
			AotHelper.EnsureList<MetadataKvpObject>();
			AotHelper.EnsureList<ModMediaObject>();
			AotHelper.EnsureList<ModfileObject>();
			AotHelper.EnsureList<ModStatsObject>();
			AotHelper.EnsureList<GameTagOptionObject>();
			AotHelper.EnsureList<ModTagObject>();
			AotHelper.EnsureList<TermsObject>();
			AotHelper.EnsureList<TermsButtonObject>();
			AotHelper.EnsureList<TermsLinksObject>();
			AotHelper.EnsureList<DownloadObject>();
			AotHelper.EnsureList<FilehashObject>();
			AotHelper.EnsureList<UserEventObject>();
			AotHelper.EnsureList<ModEventObject>();
			AotHelper.EnsureList<AvatarObject>();
			AotHelper.EnsureList<AccessTokenObject>();
			AotHelper.EnsureList<ErrorObject>();
			AotHelper.EnsureList<Error>();
			AotHelper.EnsureList<ModCollectionRegistry>();
			AotHelper.EnsureList<UserModCollectionData>();
			AotHelper.EnsureList<ModCollectionEntry>();
			AotHelper.EnsureList<ModProfile>();
			AotHelper.EnsureList<DownloadReference>();
			AotHelper.EnsureList<KeyValuePair<string, string>>();
			AotHelper.EnsureList<ModStats>();
			AotHelper.EnsureList<DateTime>();
			AotHelper.EnsureList<RatingObject>();
			AotHelper.EnsureList<ModDependenciesObject>();
			AotHelper.EnsureList<GameMonetizationTeamObject>();
			AotHelper.EnsureList<MonetizationTeamAccountsObject>();

            // Marketplace and IAP
            AotHelper.EnsureList<EntitlementObject>();
            AotHelper.EnsureList<EntitlementWalletObject>();
            AotHelper.EnsureList<EntitlementDetailsObject>();
            AotHelper.EnsureList<WalletObject>();
            AotHelper.EnsureList<CheckoutProcessObject>();

			// Wss messages
			AotHelper.EnsureList<WssMessage>();
			AotHelper.EnsureList<WssMessages>();

			// Wss objects
			AotHelper.EnsureList<WssLoginSuccess>();
			AotHelper.EnsureList<WssErrorObject>();
			AotHelper.EnsureList<WssDeviceLoginRequest>();
			AotHelper.EnsureList<WssDeviceLoginResponse>();

		}
	}
}
