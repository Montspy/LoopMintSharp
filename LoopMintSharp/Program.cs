﻿using LoopMintSharp;
using Newtonsoft.Json;
using PoseidonSharp;
using System.Numerics;
using System.Text;
using Multiformats.Hash;

string apiKey = Environment.GetEnvironmentVariable("LOOPRINGAPIKEY", EnvironmentVariableTarget.Machine);//you can either set an environmental variable or input it here directly.
string loopringPrivateKey = Environment.GetEnvironmentVariable("LOOPRINGPRIVATEKEY", EnvironmentVariableTarget.Machine); //you can either set an environmental variable or input it here directly.
string ethereumPrivateKey = Environment.GetEnvironmentVariable("ETHEREUMPRIVATEKEY", EnvironmentVariableTarget.Machine); //you can either set an environmental variable or input it here directly.

//Console.WriteLine(apiKey);
//Console.WriteLine(loopringPrivateKey);
//Console.WriteLine(ethereumPrivateKey);

ILoopringMintService loopringMintService = new LoopringMintService();
var storageId = await loopringMintService.GetNextStorageId(apiKey, 40940, 0);
Console.WriteLine($"Storage id: {JsonConvert.SerializeObject(storageId, Formatting.Indented)}");

CounterFactualNftInfo counterFactualNftInfo = new CounterFactualNftInfo
{
    nftOwner = "0x36Cd6b3b9329c04df55d55D41C257a5fdD387ACd",
    nftFactory = "0xc852aC7aAe4b0f0a0Deb9e8A391ebA2047d80026",
    nftBaseUri = ""
};

//Generating the nftId
var ipfsCid = "QmQNhJjDGaugoWRWktS6s6SYQcVRmxwmdx9Q34zy6PhtR8"; // kkb nft that has already been minted
Multihash multiHash = Multihash.Parse(ipfsCid);
string multiHashString = multiHash.ToString();
var ipfsCidBigInteger = Utils.ParseHexUnsigned(multiHashString);
var nftId = "0x" + ipfsCidBigInteger.ToString("x").Substring(4);


var exchange = "0x0BABA1Ad5bE3a5C0a66E7ac838a129Bf948f1eA4";
var minterAddress = "0x36Cd6b3b9329c04df55d55D41C257a5fdD387ACd";
var accountId = 40940;
var nftType = 0;
var creatorFeeBips = 0;
var amount = 1;
var validUntil = 1700000000;

var counterFactualNft = await loopringMintService.ComputeTokenAddress(apiKey, counterFactualNftInfo);
Console.WriteLine($"CounterFactualNFT Token Address: {JsonConvert.SerializeObject(counterFactualNft, Formatting.Indented)}");

var offChainFee = await loopringMintService.GetOffChainFee(apiKey, 40940, 9, counterFactualNft.tokenAddress);
Console.WriteLine($"Offchain fee: {JsonConvert.SerializeObject(offChainFee, Formatting.Indented)}");


var nftIdHi = Utils.ParseHexUnsigned(nftId.Substring(0,34));
var nftIdLo = Utils.ParseHexUnsigned(nftId.Substring(34, 32));
BigInteger[] nftDataPoseidonInputs = 
{
    Utils.ParseHexUnsigned(minterAddress),
    (BigInteger) 0,
    Utils.ParseHexUnsigned(counterFactualNft.tokenAddress),
    nftIdLo,
    nftIdHi,
    (BigInteger)creatorFeeBips
};
Poseidon nftDataPoseidon = new Poseidon(7, 6, 52, "poseidon", 5, _securityTarget: 128);
BigInteger nftDataPoseidonHash = nftDataPoseidon.CalculatePoseidonHash(nftDataPoseidonInputs);


BigInteger[] nftPoseidonInputs =
{
    Utils.ParseHexUnsigned(exchange),
    (BigInteger) accountId,
    (BigInteger) accountId,
    nftDataPoseidonHash,
    (BigInteger) amount,
    (BigInteger) 0,
    BigInteger.Parse(offChainFee.fees[0].fee),
    (BigInteger) validUntil,
    (BigInteger) storageId.offchainId
};

Poseidon nftPoseidon = new Poseidon(10, 6, 53, "poseidon", 5, _securityTarget: 128);
BigInteger nftPoseidonHash = nftPoseidon.CalculatePoseidonHash(nftPoseidonInputs);
Eddsa eddsa = new Eddsa(nftPoseidonHash, loopringPrivateKey);
string eddsaSignature = eddsa.Sign();

var nftMintResponse = await loopringMintService.MintNft(
    apiKey: apiKey,
    exchange: exchange,
    minterId: accountId,
    minterAddress: minterAddress,
    toAccountId: accountId,
    toAddress: minterAddress,
    nftType: 0,
    tokenAddress: counterFactualNft.tokenAddress,
    nftId,
    amount: amount.ToString(),
    validUntil: validUntil,
    creatorFeeBips: creatorFeeBips,
    storageId.offchainId,
    maxFeeTokenId: 0,
    maxFeeAmount: offChainFee.fees[0].fee,
    forceToMint: false,
    counterFactualNftInfo: counterFactualNftInfo,
    eddsaSignature: eddsaSignature
    );
Console.WriteLine($"Nft Mint response: {JsonConvert.SerializeObject(nftMintResponse, Formatting.Indented)}");

Console.WriteLine("Enter any key to exit");
Console.ReadKey();