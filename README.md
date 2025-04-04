# Pet AI
This Vintage Story mod is serving as a library for my other animal taming mods. 

<br>

## API Information

If you want use this to create your own pets, be sure to check out [wolftaming](https://github.com/G3rste/wolftaming) and [cats](https://github.com/G3rste/cats). 

<br>

### Behaviors and AITasks

Here is a short overview over the most important new entity behaviors and tasks:

* **behaviors**:
  * **tameable**: indicates that an entity is tameable, contains the following attributes
    * **size**: the size of the pet used to determine the size of their petcushion, valid values are small | medium | large
    * **disobediencePerDay**: Indicates in percent how much disobedience (likelyhood your pet wont follow orders) the pet gains per day if not getting care
    * **treat**: list of treats you can feed your pet with to tame it/ increase its obedience, eacht treat contains the following attributes
      * **code**: code of the item 
      * **domain**: mod domain of the item
      * **progress**: how many percent of taming progress/ obedience does the pet gain when receiving this treat
      * **cooldown**: cooldown in in-game hours for how long your pet will need to accept food again
  * **receivecommand**: indicates that the pet can be trained to execute certain commands
    * **availablecommands**": there are a couple commands of various complexity available for your pet, here is an examplelist: ```{ "commandName":"sit", "commandType":"SIMPLE", "minObedience":0.2 }, { "commandName":"lay", "commandType":"SIMPLE", "minObedience":0.2 }, { "commandName":"speak", "commandType":"SIMPLE", "minObedience":0.2 }, { "commandName":"followmaster", "commandType":"COMPLEX", "minObedience":0.6 }, { "commandName":"stay", "commandType":"COMPLEX", "minObedience":0.1 }, { "commandName":"NEUTRAL", "commandType":"AGGRESSIONLEVEL", "minObedience":0 }, { "commandName":"PROTECTIVE", "commandType":"AGGRESSIONLEVEL", "minObedience":0.5 }, { "commandName":"AGGRESSIVE", "commandType":"AGGRESSIONLEVEL", "minObedience":0.7 }, { "commandName":"PASSIVE", "commandType":"AGGRESSIONLEVEL", "minObedience":0.8 }```
  * **raisable**: same as the vanilla behavior grow, but will keep the pets taming stats on growing up
* **aitasks**:
  * **petmeleeattack**: basically the same as the vanilla meleeattack, but necessary if your pet should fight alongside you
    * **isCommandable**: if set to true, your pet can aid you an combat and react to your commands
  * **petseekentity**: basically the same as the vanilla meleeattack, but necessary if your pet should fight alongside you
    * **isCommandable**: if set to true, your pet can aid you an combat and react to your commands
  * **simplecommand**: lets your pet play an animation on command, useful for implementing things like sit, speak, flip, has the same attributes as the vanilla idle task
  * **followmaster**: necessary for your pet to be able to execute the follow command, has the same attributes as the vanilla staycloseto task
  * **stay**: necessary for your pet to be able to execute the stay command, has the same attributes as the vanilla staycloseto task
  * **seeknest**: lets your pet seek its cushion at certain times of day, contains attibutes of vanilla idle task

<br>

### Pet Accessories

Your pets can also wear accessories, armor and backpacks (see [dog collar](https://github.com/G3rste/wolftaming/blob/main/resources/assets/wolftaming/itemtypes/dogcollar.json)). Since 1.20 this uses the vanilla mechanics to implement this (see the tamed elk for more reference).

If the item should also work as armor, you cann add the damageReduction attribute (lets you set the damageReduction in percent, see [dog armor](https://github.com/G3rste/wolftaming/blob/main/resources/assets/wolftaming/itemtypes/dogarmor.json)).

![Thumbnail](petai.png)