[中文](Readme-cn.md)
# **A Value-Co-Creation Ecosystem**
##### OX is a perpetual, collaborative, mutual-assistive, and co-creative trustable value ecosystem constructed using blockchain and smart contract technologies. Its underlying technology employs the mature, reliable, and high-performance upgraded Neo protocol, incorporating the DBFT consensus algorithm, the NeoVM virtual machine, and the Neo smart contract mechanism.

## Flash Message
Flash Message is one of the primary design objectives of the OX ecosystem, aiming to fully leverage the redundant broadcast resources of the blockchain P2P network to construct a secure, reliable, and censorship-resistant end-to-end encrypted social network. All broadcast data in Flash Message, akin to asset transactions, require signature verification and are broadcasted through node relaying. However, none of the data is packaged into blocks, nor is it subject to validator node interference. Blockchain P2P network resources prioritize on-chain asset transactions, with surplus resources allocated to broadcasting Flash Messages data. This distribution to differing user addresses occurs through native smart contracts, where addresses holding more OXC assets gain higher frequency message-sending privileges.

## Ethereum Map
The OX ecosystem introduces Ethereum's signature rules as an auxiliary verification mechanism, while native smart contracts accommodate each Ethereum address with a unique OX mapping address. Ethereum accounts manage assets on their mapping addresses through their signatures, thereby achieving compatibility with Ethereum accounts within the OX ecosystem.

## NFT
Non-Fungible Token (NFT) is another primary design goal of the OX ecosystem. Any address can mint specific NFTs on the blockchain and issue several instances of an NFT using the minted NFT's metadata. NFT metadata may be stored directly on the blockchain in a compact size or only store the NFT data's CID on-chain, with the NFT data itself stored on a decentralized storage network like IPFS. NFT instances can be traded in a C2C manner without centralized assurances. Any holder of an NFT instance can sign a sales order specifying the NFT instance ID and sale price. Buyers use this sales order to construct a transfer transaction of the NFT instance. If the sale price is paid during the transaction, the transfer transaction will be validated and packaged into a block, completing the transfer of the NFT instance. The NFT instance's sales order is essentially an offline data signature that can be transmitted in any manner, such as email, mail, and especially via Flash Message broadcasts.

## Slot
Slot is one of the OX ecosystem's principal design objectives, serving as the main avenue for implementing service-oriented decentralized business logic and as the cornerstone for a trustworthy asset custody protocol. Any OX address can register as a Slot on the blockchain, using a series of mapped smart contract addresses within the Slot to custody clients' assets and manage them according to public rules. Slots rent fixed block durations by paying GAS and must maintain flexible pledging of at least 1,000,000 OXS during the rental period. Failure to meet the duration or OXS holding threshold results in Slot invalidation, meaning the Slot has no authority over the custodied assets, effectively freezing them. Additionally, any address holding OXS shares can permanently ban a Slot through a lock-up vote, requiring a cumulative OXS vote count exceeding 50,000,000 OXS.

## Event
Event aims to provide an omnipresent log function, allowing any address to construct an Event transaction by paying a small GAS fee. This enables immutable recording of temporal and spatial events on the blockchain for transparent public viewing, allowing other addresses to comment on these events.

## Lock Asset
The OX ecosystem offers a native smart contract to facilitate temporary asset locking until a specified expiry time or block height. Locking assets generally benefits the consolidation and enhancement of token consensus and the development of custom token economic models.

## Lock Vote
Asset locking over the same specific block heights offers a straightforward DAO consensus voting method, preventing repeated voting during the lock-up period.

## Trust Asset
The OX ecosystem provides a native smart contract for implementing secure and trustworthy asset trust functions. The settlor appoints a trustee and specifies the trust scope and trust Slot, thus forming a specific trust smart contract address. The assets on this trust address can be freely managed by the trustee, with asset transfers limited to addresses within the trust scope and the managed mapping addresses of the trust Slot. This strict trust smart contract prevents misappropriation by the trustee while ensuring their right to freely manage the assets.

## Native Tokens
The OX ecosystem incorporates two native tokens, OXS and OXC. OXS is an equity token representing ownership and managerial rights over the OX ecosystem, with a total quantity of 100 million shares. Ownership includes rights to vote on consensus node elections and network parameter changes. OXS is indivisible, with the smallest unit being one.

OXC serves as the GAS (fuel token), with a maximum total cap of 1.5 billion, used for resource control during OX network operation. The OX network charges for token transfers and the operation and storage of smart contracts, thereby economically incentivizing consensus nodes and preventing resource misuse. GAS's smallest unit is 0.00000001.

In the OX network's genesis block, 100 million OXS have been created, while OXC has not yet been generated, amounting to zero. The 1.5 billion OXC corresponding to the 100 million OXS will gradually be generated to the OXS equity token addresses over approximately 50 years through a decaying algorithm. OXC generated at new addresses will continue following an OXS equity token's transfer.

The total amount and allocation plan of OXC tokens can be adjusted in each 20,000,000-block cycle through a joint lock-up vote by OXS holding addresses, effective only if the voting OXS quantity exceeds half of the total OXS. In the absence of adequate voting, the pre-set token generation plan for the next cycle will proceed.

## OXS Distribution Mechanism
1. 20% is gradually locked and managed by the foundation to incentivize OX core developers, peripheral ecosystem developers, and council members in the future.
2. 20% is custodied by the foundation and airdropped in batches to Flash Message users within two years.
3. 10% is injected into OXS's decentralized trading pool to support sufficient trading pair depth.
4. 10% supports the flexible pledging of OXS for the 10 official Slots to maintain their availability and effectiveness.
5. 20% is awarded proportionally to addresses from the testnet balance snapshot.
6. 10% is initially sold to cover returns on investments from the OX project's seed and angel rounds.
7. 10% is used for cross-chain token swaps to help enhance the market value of the OX ecosystem.
