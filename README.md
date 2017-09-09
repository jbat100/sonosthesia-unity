# Sonosthesia-Unity

Part of the [Sonosthesia](http://www.sonosthesia.com/home) project.

## Project Overview

### Core

Core networking and messaging functionality is provided to expose **input** and **output**, **static** and **dynamic** channels to the [Sonosthsia-Hub](https://github.com/jbat100/sonosthesia-hub/tree/develop) (see [Sonosthesia Protocol](http://www.sonosthesia.com/protocols)) using game objects in the Unity editor. Eventually, the ability to create new channels dynamically at run time from user input actions (either VR or Desktop) may be added.

### Scene Controllers and Modifiers

Scene controllers and modifiers allow **input** channels (see [Sonosthesia Protocol](http://www.sonosthesia.com/protocols)) to be used to control and modify Unity game objects in the scene at run time. This is a work in progress and a large number of different options should be provided, with particular effort given to reusability and genericity. 

### Interaction Descriptors

Interaction descriptors allow user input (in VR or Desktop) to be pushed to **output** channels (see [Sonosthesia Protocol](http://www.sonosthesia.com/protocols)). There objective is to provide intuitive, organic, effective, highly dimensional output (control) data to other components through the [Sonosthsia-Hub](https://github.com/jbat100/sonosthesia-hub/tree/develop).

## Why Unity

A number of game engines are available today, but Unity stands out as the environment of choice for virtual reality. Its extensive C# scripting capabilities and hooks for binary extensions give it, on top of all its advantages, a great opportunity to implement custom behaviour. A networking engine implementing the protocols developed for the project is proposed, as well as ongoing work on visualisation and collision parameterisation for sound synthesis control.

Mainstream game engines such as Unity offer game developers and artists functionality which is of obvious interest for Sonosthesia, such as advanced scene editing, guizmos, particle systems, a great shader language, powerful profiling and importantly a hugely abundant and versatile resource in the online asset store. It also produces extremely portable and adaptable software, which will work with a wide array of input devices, exploit the latest advances in GPU hardware and run on a wide variety of platforms including Windows, Linux, macOS, iOS, Android, WebGL and most importantly emerging head mounted displays aiming to democratise virtual reality (Occulus Rift, HTC Vive, Sumsung GearVR). Being supported by a powerful gaming industry, Unity seems like it is bound to surf the crest of the VR wave at least for the foreseeable future.

