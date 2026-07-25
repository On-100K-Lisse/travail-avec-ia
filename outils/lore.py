# -*- coding: latin-1 -*-
# Table officielle Lore-Accurate : dossier -> (tier, classe M2)
# Tiers : stats + cible de reaction. Classe : profil de garde/distances.
LORE = {
 # ---- T0 : entites cosmiques (2400/130/130, reaction 381) ----
 "kirby8":("T0","Polyvalent"), "Kratos":("T0","Cogneur"), "ssbbmasterhand":("T0","Zoner"),
 "Mephiles (DBS)":("T0","Stratege"), "Sonic.Exe":("T0","Rapide"),
 "Shadic":("T0","Polyvalent"), "Shadic v2":("T0","Polyvalent"), "Sonow (Canon)":("T0","Polyvalent"),
 "Solaire":("T0","Polyvalent"),                      # absent de la liste : tier conserve
 # ---- T1 : menaces planetaires (1900/125/120, reaction 353) ----
 "BowserSuminI":("T1","Cogneur"), "CloudMYTHOS":("T1","Polyvalent"),
 "Devil Mario":("T1","Polyvalent"), "Marigi":("T1","Polyvalent"),
 "Emerl (2)":("T1","Polyvalent"), "GanonMYTHOS":("T1","Cogneur"), "HeroXI":("T1","Polyvalent"),
 "Nazo":("T1","Rapide"), "Seelkadoom (New)":("T1","Rapide"), "Sonic Black V3 Nitro":("T1","Rapide"),
 "Seethe":("T1","Rapide"), "Kamba The Hedgehog":("T1","Cogneur"),
 "Neo Metal Sonic":("T1","Rapide"),
 # ---- T2 : destructeurs de villes (1600/115/115, reaction 325) ----
 "Blaze The Cat":("T2","Rapide"), "BowserJrSuminI":("T2","Cogneur"), "BOWSER2":("T2","Cogneur"),
 "Dark Sonic":("T2","Rapide"), "Dark Shadow":("T2","Rapide"), "DLinkMYTHOS":("T2","Rapide"),
 "MvC_DK":("T2","Cogneur"), "Fleetway Sonic":("T2","Rapide"), "Fleetway Shadow1":("T2","Rapide"),
 "Pit":("T2","Zoner"), "DarkPit":("T2","Zoner"), "Scourge":("T2","Rapide"), "Silver":("T2","Zoner"),
 # ---- T3 : le standard (reaction 282) ----
 "Shadow JUS Updated":("T3","Polyvalent"), "Mario":("T3","Polyvalent"), "Luigi":("T3","Polyvalent"),
 "Shade":("T3","Polyvalent"), "Mecha_Mario_TB":("T3","Polyvalent"), "MechaSonic":("T3","Polyvalent"),
 "MarioMYTHOS":("T3","Polyvalent"), "LuigiMYTHOS":("T3","Polyvalent"), "ZeldaMYTHOS":("T3","Polyvalent"),
 "Rayman":("T3","Polyvalent"), "Sonic V1.0 JUS AI Patch":("T3","Polyvalent"),
 "Sonic_Nitro":("T3","Rapide"), "Classic Sonic":("T3","Rapide"), "Espio_The_Chameleon_TB":("T3","Rapide"),
 "Greninja":("T3","Rapide"), "Metal Sonic":("T3","Rapide"), "MetalSonic":("T3","Rapide"),
 "Metal Scourge":("T3","Rapide"), "jetthehawk":("T3","Rapide"), "Puss in Boots":("T3","Rapide"),
 "Altair":("T3","Rapide"),
 "Knuckles V2":("T3","Cogneur"), "Amy Rose":("T3","Cogneur"), "Hommer Mario":("T3","Cogneur"),
 "Metal_Knuckles_JUS_TB":("T3","Cogneur"), "mightysvp":("T3","Cogneur"),
 "metalknuckles":("T3","Cogneur"),                   # 2e Metal Knuckles : tier conserve
 "Fox_Mccloud":("T3","Zoner"), "Link":("T3","Zoner"), "Link_k":("T3","Zoner"),
 "Fire Sonic":("T3","Zoner"), "Cream The Rabbit":("T3","Zoner"), "Solid_Snake":("T3","Zoner"),
 "ChibiSolid Snake":("T3","Zoner"), "Wave":("T3","Zoner"), "Vector":("T3","Zoner"), "Aeon":("T3","Zoner"),
 "Tails":("T3","Stratege"), "Android Shadow":("T3","Stratege"), "Metal Shadow":("T3","Stratege"),
}
# stats par tier : le sous-tier ne module QUE le T3 (regle : personne sous 1250)
STATS = {
 "T0": lambda cl: (2400,130,130),
 "T1": lambda cl: (1900,125,120),
 "T2": lambda cl: (1600,115,115),
 "T3": lambda cl: {"Cogneur":(1450,115,115),"Rapide":(1250,105,100),
                   "Zoner":(1250,105,95),"Stratege":(1300,100,105)}.get(cl,(1350,105,105)),
}
REACTION={"T0":381,"T1":353,"T2":325,"T3":282.5}
# coefficient de vitesse : seulement au T3 (les tiers superieurs gardent leur mobilite d'origine)
VITESSE={"Rapide":1.08,"Cogneur":0.90}
# taux de garde par classe (1 chance sur N)
GARDE={"Cogneur":1.6,"Rapide":8,"Zoner":4,"Polyvalent":3,"Stratege":2.5}
