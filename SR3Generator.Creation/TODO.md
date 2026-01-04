# TODO list for SR3Generator CharacterBuilder

[x] Add Banking

[x] Add Karma tracking

[x] Add more Gear types
	- Vehicle
	- Cyberdeck
		- include Persona properties, hacking pool
	- Program

[x] Add Contacts

[x] Add Foci

[x] Add Spells (validations?)

[] Add other magic stuff?

[] Implement modifications (in 2 phases)
	1. Just add modifications as a subtype of Equipment, added like any other gear.
		- Grouping/organizing mods becomes a UI problem
	2. Implement a modification property for slot, and a property to gear for slot capacity.

[] Data loading from json in Database classes
	When ingesting vehicles, be mindful of differing stats, and ignore vehicles from sr2

[x] Add logs for all unexpected returns in CharacterBuilder

## Phase 2 features

[] Advanced Lifestyle stuff

[] Cyberterminal construction builder (similar to character builder?)

[] Vehicle construction builder

[] Ally Spirits

### Shadowrun Companion

[] Point-based character creation

[] Add Edges/Flaws