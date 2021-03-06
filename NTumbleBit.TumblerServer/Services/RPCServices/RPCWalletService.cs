﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NBitcoin;
using NBitcoin.RPC;
using Newtonsoft.Json.Linq;
using NTumbleBit.PuzzlePromise;
using NBitcoin.DataEncoders;

#if !CLIENT
namespace NTumbleBit.TumblerServer.Services.RPCServices
#else
namespace NTumbleBit.Client.Tumbler.Services.RPCServices
#endif
{
	public class RPCWalletService : IWalletService
	{
		public RPCWalletService(RPCClient rpc)
		{
			if(rpc == null)
				throw new ArgumentNullException(nameof(rpc));
			_RPCClient = rpc;
		}

		private readonly RPCClient _RPCClient;
		public RPCClient RPCClient
		{
			get
			{
				return _RPCClient;
			}
		}

		public IDestination GenerateAddress(string label)
		{
			var result = _RPCClient.SendCommand("getnewaddress", label ?? "");
			return BitcoinAddress.Create(result.ResultString, _RPCClient.Network);
		}

		public Coin AsCoin(UnspentCoin c)
		{
			var coin = new Coin(c.OutPoint, new TxOut(c.Amount, c.ScriptPubKey));
			if(c.RedeemScript != null)
				coin = coin.ToScriptCoin(c.RedeemScript);
			return coin;
		}

		public Transaction FundTransaction(TxOut txOut, FeeRate feeRate)
		{
			Transaction tx = new Transaction();
			tx.Outputs.Add(txOut);

			var changeAddress = BitcoinAddress.Create(_RPCClient.SendCommand("getrawchangeaddress").ResultString, _RPCClient.Network);

			FundRawTransactionResponse response = null;
			try
			{
				response = _RPCClient.FundRawTransaction(tx, new FundRawTransactionOptions()
				{
					ChangeAddress = changeAddress,
					FeeRate = feeRate,
					LockUnspents = true
				});
			}
			catch(RPCException ex)
			{
				if(ex.Message.Equals("Insufficient funds", StringComparison.OrdinalIgnoreCase))
					return null;
				throw;
			}
			var result = _RPCClient.SendCommand("signrawtransaction", response.Transaction.ToHex());
			return new Transaction(((JObject)result.Result)["hex"].Value<string>());
		}
	}
}
