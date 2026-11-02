-- Puzzle Bobble 4 PopTracker AP auto-tracking.
-- Uses actual IDs from the Puzzle Bobble 4 .apworld.
Tracker:AddMaps("maps/maps.json")
Tracker:AddItems("items/items.json")
Tracker:AddLayouts("layouts/default.json")
Tracker:AddLayouts("layouts/map_tabs.json")

local location_map = {
    [167000] = "@World A/A1",
    [167001] = "@World A/A2",
    [167002] = "@World A/A3",
    [167003] = "@World A/A4",
    [167004] = "@World A/A5",
    [167005] = "@World B/B1",
    [167006] = "@World B/B2",
    [167007] = "@World B/B3",
    [167008] = "@World B/B4",
    [167009] = "@World B/B5",
    [167010] = "@World C/C1",
    [167011] = "@World C/C2",
    [167012] = "@World C/C3",
    [167013] = "@World C/C4",
    [167014] = "@World C/C5",
    [167015] = "@World D/D1",
    [167016] = "@World D/D2",
    [167017] = "@World D/D3",
    [167018] = "@World D/D4",
    [167019] = "@World D/D5",
    [167020] = "@World E/E1",
    [167021] = "@World E/E2",
    [167022] = "@World E/E3",
    [167023] = "@World E/E4",
    [167024] = "@World E/E5",
    [167025] = "@World F/F1",
    [167026] = "@World F/F2",
    [167027] = "@World F/F3",
    [167028] = "@World F/F4",
    [167029] = "@World F/F5",
    [167030] = "@World G/G1",
    [167031] = "@World G/G2",
    [167032] = "@World G/G3",
    [167033] = "@World G/G4",
    [167034] = "@World G/G5",
    [167035] = "@World H/H1",
    [167036] = "@World H/H2",
    [167037] = "@World H/H3",
    [167038] = "@World H/H4",
    [167039] = "@World H/H5",
    [167040] = "@World I/I1",
    [167041] = "@World I/I2",
    [167042] = "@World I/I3",
    [167043] = "@World I/I4",
    [167044] = "@World I/I5",
    [167045] = "@World J/J1",
    [167046] = "@World J/J2",
    [167047] = "@World J/J3",
    [167048] = "@World J/J4",
    [167049] = "@World J/J5",
    [167050] = "@World K/K1",
    [167051] = "@World K/K2",
    [167052] = "@World K/K3",
    [167053] = "@World K/K4",
    [167054] = "@World K/K5",
    [167055] = "@World L/L1",
    [167056] = "@World L/L2",
    [167057] = "@World L/L3",
    [167058] = "@World L/L4",
    [167059] = "@World L/L5",
    [167060] = "@World M/M1",
    [167061] = "@World M/M2",
    [167062] = "@World M/M3",
    [167063] = "@World M/M4",
    [167064] = "@World M/M5",
    [167065] = "@World N/N1",
    [167066] = "@World N/N2",
    [167067] = "@World N/N3",
    [167068] = "@World N/N4",
    [167069] = "@World N/N5",
    [167070] = "@World O/O1",
    [167071] = "@World O/O2",
    [167072] = "@World O/O3",
    [167073] = "@World O/O4",
    [167074] = "@World O/O5",
    [167075] = "@World P/P1",
    [167076] = "@World P/P2",
    [167077] = "@World P/P3",
    [167078] = "@World P/P4",
    [167079] = "@World P/P5",
    [167080] = "@World Q/Q1",
    [167081] = "@World Q/Q2",
    [167082] = "@World Q/Q3",
    [167083] = "@World Q/Q4",
    [167084] = "@World Q/Q5",
    [167085] = "@World R/R1",
    [167086] = "@World R/R2",
    [167087] = "@World R/R3",
    [167088] = "@World R/R4",
    [167089] = "@World R/R5",
    [167090] = "@World S/S1",
    [167091] = "@World S/S2",
    [167092] = "@World S/S3",
    [167093] = "@World S/S4",
    [167094] = "@World S/S5",
    [167095] = "@World T/T1",
    [167096] = "@World T/T2",
    [167097] = "@World T/T3",
    [167098] = "@World T/T4",
    [167099] = "@World T/T5",
    [167100] = "@World U/U1",
    [167101] = "@World U/U2",
    [167102] = "@World U/U3",
    [167103] = "@World U/U4",
    [167104] = "@World U/U5",
    [167105] = "@World V/V1",
    [167106] = "@World V/V2",
    [167107] = "@World V/V3",
    [167108] = "@World V/V4",
    [167109] = "@World V/V5",
    [167110] = "@World W/W1",
    [167111] = "@World W/W2",
    [167112] = "@World W/W3",
    [167113] = "@World W/W4",
    [167114] = "@World W/W5",
    [167115] = "@World ?/?1",
    [167116] = "@World ?/?2",
    [167117] = "@World ?/?3",
    [167118] = "@World ?/?4",
    [167119] = "@World ?/?5",
    [167120] = "@World X/X1",
    [167121] = "@World X/X2",
    [167122] = "@World X/X3",
    [167123] = "@World X/X4",
    [167124] = "@World X/X5",
    [167125] = "@World Y/Y1",
    [167126] = "@World Y/Y2",
    [167127] = "@World Y/Y3",
    [167128] = "@World Y/Y4",
    [167129] = "@World Y/Y5",
    [167130] = "@World Z/Z1",
    [167131] = "@World Z/Z2",
    [167132] = "@World Z/Z3",
    [167133] = "@World Z/Z4",
    [167134] = "@World Z/Z5",
}



-- Puzzle/Story Tarot level checks (22 cards x 5 levels).
local tarot_location_map = {
    [167200] = "@Tarot Fool/Fool 1",
    [167201] = "@Tarot Fool/Fool 2",
    [167202] = "@Tarot Fool/Fool 3",
    [167203] = "@Tarot Fool/Fool 4",
    [167204] = "@Tarot Fool/Fool 5",
    [167205] = "@Tarot Star/Star 1",
    [167206] = "@Tarot Star/Star 2",
    [167207] = "@Tarot Star/Star 3",
    [167208] = "@Tarot Star/Star 4",
    [167209] = "@Tarot Star/Star 5",
    [167210] = "@Tarot Moon/Moon 1",
    [167211] = "@Tarot Moon/Moon 2",
    [167212] = "@Tarot Moon/Moon 3",
    [167213] = "@Tarot Moon/Moon 4",
    [167214] = "@Tarot Moon/Moon 5",
    [167215] = "@Tarot Hermit/Hermit 1",
    [167216] = "@Tarot Hermit/Hermit 2",
    [167217] = "@Tarot Hermit/Hermit 3",
    [167218] = "@Tarot Hermit/Hermit 4",
    [167219] = "@Tarot Hermit/Hermit 5",
    [167220] = "@Tarot Tower/Tower 1",
    [167221] = "@Tarot Tower/Tower 2",
    [167222] = "@Tarot Tower/Tower 3",
    [167223] = "@Tarot Tower/Tower 4",
    [167224] = "@Tarot Tower/Tower 5",
    [167225] = "@Tarot Chariot/Chariot 1",
    [167226] = "@Tarot Chariot/Chariot 2",
    [167227] = "@Tarot Chariot/Chariot 3",
    [167228] = "@Tarot Chariot/Chariot 4",
    [167229] = "@Tarot Chariot/Chariot 5",
    [167230] = "@Tarot Death/Death 1",
    [167231] = "@Tarot Death/Death 2",
    [167232] = "@Tarot Death/Death 3",
    [167233] = "@Tarot Death/Death 4",
    [167234] = "@Tarot Death/Death 5",
    [167235] = "@Tarot Devil/Devil 1",
    [167236] = "@Tarot Devil/Devil 2",
    [167237] = "@Tarot Devil/Devil 3",
    [167238] = "@Tarot Devil/Devil 4",
    [167239] = "@Tarot Devil/Devil 5",
    [167240] = "@Tarot Hanged Man/Hanged Man 1",
    [167241] = "@Tarot Hanged Man/Hanged Man 2",
    [167242] = "@Tarot Hanged Man/Hanged Man 3",
    [167243] = "@Tarot Hanged Man/Hanged Man 4",
    [167244] = "@Tarot Hanged Man/Hanged Man 5",
    [167245] = "@Tarot Magician/Magician 1",
    [167246] = "@Tarot Magician/Magician 2",
    [167247] = "@Tarot Magician/Magician 3",
    [167248] = "@Tarot Magician/Magician 4",
    [167249] = "@Tarot Magician/Magician 5",
    [167250] = "@Tarot Wheel of Fortune/Wheel of Fortune 1",
    [167251] = "@Tarot Wheel of Fortune/Wheel of Fortune 2",
    [167252] = "@Tarot Wheel of Fortune/Wheel of Fortune 3",
    [167253] = "@Tarot Wheel of Fortune/Wheel of Fortune 4",
    [167254] = "@Tarot Wheel of Fortune/Wheel of Fortune 5",
    [167255] = "@Tarot Judgement/Judgement 1",
    [167256] = "@Tarot Judgement/Judgement 2",
    [167257] = "@Tarot Judgement/Judgement 3",
    [167258] = "@Tarot Judgement/Judgement 4",
    [167259] = "@Tarot Judgement/Judgement 5",
    [167260] = "@Tarot Lovers/Lovers 1",
    [167261] = "@Tarot Lovers/Lovers 2",
    [167262] = "@Tarot Lovers/Lovers 3",
    [167263] = "@Tarot Lovers/Lovers 4",
    [167264] = "@Tarot Lovers/Lovers 5",
    [167265] = "@Tarot Temperance/Temperance 1",
    [167266] = "@Tarot Temperance/Temperance 2",
    [167267] = "@Tarot Temperance/Temperance 3",
    [167268] = "@Tarot Temperance/Temperance 4",
    [167269] = "@Tarot Temperance/Temperance 5",
    [167270] = "@Tarot Hierophant/Hierophant 1",
    [167271] = "@Tarot Hierophant/Hierophant 2",
    [167272] = "@Tarot Hierophant/Hierophant 3",
    [167273] = "@Tarot Hierophant/Hierophant 4",
    [167274] = "@Tarot Hierophant/Hierophant 5",
    [167275] = "@Tarot Emperor/Emperor 1",
    [167276] = "@Tarot Emperor/Emperor 2",
    [167277] = "@Tarot Emperor/Emperor 3",
    [167278] = "@Tarot Emperor/Emperor 4",
    [167279] = "@Tarot Emperor/Emperor 5",
    [167280] = "@Tarot Empress/Empress 1",
    [167281] = "@Tarot Empress/Empress 2",
    [167282] = "@Tarot Empress/Empress 3",
    [167283] = "@Tarot Empress/Empress 4",
    [167284] = "@Tarot Empress/Empress 5",
    [167285] = "@Tarot High Priestess/High Priestess 1",
    [167286] = "@Tarot High Priestess/High Priestess 2",
    [167287] = "@Tarot High Priestess/High Priestess 3",
    [167288] = "@Tarot High Priestess/High Priestess 4",
    [167289] = "@Tarot High Priestess/High Priestess 5",
    [167290] = "@Tarot Sun/Sun 1",
    [167291] = "@Tarot Sun/Sun 2",
    [167292] = "@Tarot Sun/Sun 3",
    [167293] = "@Tarot Sun/Sun 4",
    [167294] = "@Tarot Sun/Sun 5",
    [167295] = "@Tarot Justice/Justice 1",
    [167296] = "@Tarot Justice/Justice 2",
    [167297] = "@Tarot Justice/Justice 3",
    [167298] = "@Tarot Justice/Justice 4",
    [167299] = "@Tarot Justice/Justice 5",
    [167300] = "@Tarot Strength/Strength 1",
    [167301] = "@Tarot Strength/Strength 2",
    [167302] = "@Tarot Strength/Strength 3",
    [167303] = "@Tarot Strength/Strength 4",
    [167304] = "@Tarot Strength/Strength 5",
    [167305] = "@Tarot World/World 1",
    [167306] = "@Tarot World/World 2",
    [167307] = "@Tarot World/World 3",
    [167308] = "@Tarot World/World 4",
    [167309] = "@Tarot World/World 5",
}


-- VS/Story Simple checks (one check per opponent).
local vs_story_simple_location_map = {
    [167400] = "@VS Story Simple Bob/Defeat Bob",
    [167401] = "@VS Story Simple Alkanet/Defeat Alkanet",
    [167402] = "@VS Story Simple Marino/Defeat Marino",
    [167403] = "@VS Story Simple Kurol/Defeat Kurol",
    [167404] = "@VS Story Simple Tamtam/Defeat Tamtam",
    [167405] = "@VS Story Simple Cleon/Defeat Cleon",
    [167406] = "@VS Story Simple G/Defeat G",
    [167407] = "@VS Story Simple Bramb/Defeat Bramb",
    [167408] = "@VS Story Simple Develon/Defeat Develon",
    [167409] = "@VS Story Simple Gigant/Defeat Gigant",
    [167410] = "@VS Story Simple Secret/Defeat Secret",
    [167411] = "@VS Story Simple Madam Luna/Defeat Madam Luna",
    [167412] = "@VS Story Simple Madam Luna/Defeat Dreg",
}


-- VS/Contest Simple checks (30 total wins).
local vs_contest_simple_location_map = {
    [168000] = "@VS/Contest Simple/Contest Win 1",
    [168001] = "@VS/Contest Simple/Contest Win 2",
    [168002] = "@VS/Contest Simple/Contest Win 3",
    [168003] = "@VS/Contest Simple/Contest Win 4",
    [168004] = "@VS/Contest Simple/Contest Win 5",
    [168005] = "@VS/Contest Simple/Contest Win 6",
    [168006] = "@VS/Contest Simple/Contest Win 7",
    [168007] = "@VS/Contest Simple/Contest Win 8",
    [168008] = "@VS/Contest Simple/Contest Win 9",
    [168009] = "@VS/Contest Simple/Contest Win 10",
    [168010] = "@VS/Contest Simple/Contest Win 11",
    [168011] = "@VS/Contest Simple/Contest Win 12",
    [168012] = "@VS/Contest Simple/Contest Win 13",
    [168013] = "@VS/Contest Simple/Contest Win 14",
    [168014] = "@VS/Contest Simple/Contest Win 15",
    [168015] = "@VS/Contest Simple/Contest Win 16",
    [168016] = "@VS/Contest Simple/Contest Win 17",
    [168017] = "@VS/Contest Simple/Contest Win 18",
    [168018] = "@VS/Contest Simple/Contest Win 19",
    [168019] = "@VS/Contest Simple/Contest Win 20",
    [168020] = "@VS/Contest Simple/Contest Win 21",
    [168021] = "@VS/Contest Simple/Contest Win 22",
    [168022] = "@VS/Contest Simple/Contest Win 23",
    [168023] = "@VS/Contest Simple/Contest Win 24",
    [168024] = "@VS/Contest Simple/Contest Win 25",
    [168025] = "@VS/Contest Simple/Contest Win 26",
    [168026] = "@VS/Contest Simple/Contest Win 27",
    [168027] = "@VS/Contest Simple/Contest Win 28",
    [168028] = "@VS/Contest Simple/Contest Win 29",
    [168029] = "@VS/Contest Simple/Contest Win 30",
}

-- VS/Story Complete checks (all 16 player characters x 13 enemies).
local vs_story_complete_location_map = {
    [167413] = "@VS Story Bub vs Bob/Defeat Bob as Bub",
    [167414] = "@VS Story Bub vs Alkanet/Defeat Alkanet as Bub",
    [167415] = "@VS Story Bub vs Marino/Defeat Marino as Bub",
    [167416] = "@VS Story Bub vs Kurol/Defeat Kurol as Bub",
    [167417] = "@VS Story Bub vs Tamtam/Defeat Tamtam as Bub",
    [167418] = "@VS Story Bub vs Cleon/Defeat Cleon as Bub",
    [167419] = "@VS Story Bub vs G/Defeat G as Bub",
    [167420] = "@VS Story Bub vs Bramb/Defeat Bramb as Bub",
    [167421] = "@VS Story Bub vs Develon/Defeat Develon as Bub",
    [167422] = "@VS Story Bub vs Gigant/Defeat Gigant as Bub",
    [167423] = "@VS Story Bub vs Secret/Defeat Secret as Bub",
    [167424] = "@VS Story Bub vs Madam Luna/Defeat Madam Luna as Bub",
    [167425] = "@VS Story Bub vs Dreg/Defeat Dreg as Bub",
    [167426] = "@VS Story Alkanet vs Bob/Defeat Bob as Alkanet",
    [167427] = "@VS Story Alkanet vs Alkanet/Defeat Alkanet as Alkanet",
    [167428] = "@VS Story Alkanet vs Marino/Defeat Marino as Alkanet",
    [167429] = "@VS Story Alkanet vs Kurol/Defeat Kurol as Alkanet",
    [167430] = "@VS Story Alkanet vs Tamtam/Defeat Tamtam as Alkanet",
    [167431] = "@VS Story Alkanet vs Cleon/Defeat Cleon as Alkanet",
    [167432] = "@VS Story Alkanet vs G/Defeat G as Alkanet",
    [167433] = "@VS Story Alkanet vs Bramb/Defeat Bramb as Alkanet",
    [167434] = "@VS Story Alkanet vs Develon/Defeat Develon as Alkanet",
    [167435] = "@VS Story Alkanet vs Gigant/Defeat Gigant as Alkanet",
    [167436] = "@VS Story Alkanet vs Secret/Defeat Secret as Alkanet",
    [167437] = "@VS Story Alkanet vs Madam Luna/Defeat Madam Luna as Alkanet",
    [167438] = "@VS Story Alkanet vs Dreg/Defeat Dreg as Alkanet",
    [167439] = "@VS Story Marino vs Bob/Defeat Bob as Marino",
    [167440] = "@VS Story Marino vs Alkanet/Defeat Alkanet as Marino",
    [167441] = "@VS Story Marino vs Marino/Defeat Marino as Marino",
    [167442] = "@VS Story Marino vs Kurol/Defeat Kurol as Marino",
    [167443] = "@VS Story Marino vs Tamtam/Defeat Tamtam as Marino",
    [167444] = "@VS Story Marino vs Cleon/Defeat Cleon as Marino",
    [167445] = "@VS Story Marino vs G/Defeat G as Marino",
    [167446] = "@VS Story Marino vs Bramb/Defeat Bramb as Marino",
    [167447] = "@VS Story Marino vs Develon/Defeat Develon as Marino",
    [167448] = "@VS Story Marino vs Gigant/Defeat Gigant as Marino",
    [167449] = "@VS Story Marino vs Secret/Defeat Secret as Marino",
    [167450] = "@VS Story Marino vs Madam Luna/Defeat Madam Luna as Marino",
    [167451] = "@VS Story Marino vs Dreg/Defeat Dreg as Marino",
    [167452] = "@VS Story Kurol vs Bob/Defeat Bob as Kurol",
    [167453] = "@VS Story Kurol vs Alkanet/Defeat Alkanet as Kurol",
    [167454] = "@VS Story Kurol vs Marino/Defeat Marino as Kurol",
    [167455] = "@VS Story Kurol vs Kurol/Defeat Kurol as Kurol",
    [167456] = "@VS Story Kurol vs Tamtam/Defeat Tamtam as Kurol",
    [167457] = "@VS Story Kurol vs Cleon/Defeat Cleon as Kurol",
    [167458] = "@VS Story Kurol vs G/Defeat G as Kurol",
    [167459] = "@VS Story Kurol vs Bramb/Defeat Bramb as Kurol",
    [167460] = "@VS Story Kurol vs Develon/Defeat Develon as Kurol",
    [167461] = "@VS Story Kurol vs Gigant/Defeat Gigant as Kurol",
    [167462] = "@VS Story Kurol vs Secret/Defeat Secret as Kurol",
    [167463] = "@VS Story Kurol vs Madam Luna/Defeat Madam Luna as Kurol",
    [167464] = "@VS Story Kurol vs Dreg/Defeat Dreg as Kurol",
    [167465] = "@VS Story Tamtam vs Bob/Defeat Bob as Tamtam",
    [167466] = "@VS Story Tamtam vs Alkanet/Defeat Alkanet as Tamtam",
    [167467] = "@VS Story Tamtam vs Marino/Defeat Marino as Tamtam",
    [167468] = "@VS Story Tamtam vs Kurol/Defeat Kurol as Tamtam",
    [167469] = "@VS Story Tamtam vs Tamtam/Defeat Tamtam as Tamtam",
    [167470] = "@VS Story Tamtam vs Cleon/Defeat Cleon as Tamtam",
    [167471] = "@VS Story Tamtam vs G/Defeat G as Tamtam",
    [167472] = "@VS Story Tamtam vs Bramb/Defeat Bramb as Tamtam",
    [167473] = "@VS Story Tamtam vs Develon/Defeat Develon as Tamtam",
    [167474] = "@VS Story Tamtam vs Gigant/Defeat Gigant as Tamtam",
    [167475] = "@VS Story Tamtam vs Secret/Defeat Secret as Tamtam",
    [167476] = "@VS Story Tamtam vs Madam Luna/Defeat Madam Luna as Tamtam",
    [167477] = "@VS Story Tamtam vs Dreg/Defeat Dreg as Tamtam",
    [167478] = "@VS Story Cleon vs Bob/Defeat Bob as Cleon",
    [167479] = "@VS Story Cleon vs Alkanet/Defeat Alkanet as Cleon",
    [167480] = "@VS Story Cleon vs Marino/Defeat Marino as Cleon",
    [167481] = "@VS Story Cleon vs Kurol/Defeat Kurol as Cleon",
    [167482] = "@VS Story Cleon vs Tamtam/Defeat Tamtam as Cleon",
    [167483] = "@VS Story Cleon vs Cleon/Defeat Cleon as Cleon",
    [167484] = "@VS Story Cleon vs G/Defeat G as Cleon",
    [167485] = "@VS Story Cleon vs Bramb/Defeat Bramb as Cleon",
    [167486] = "@VS Story Cleon vs Develon/Defeat Develon as Cleon",
    [167487] = "@VS Story Cleon vs Gigant/Defeat Gigant as Cleon",
    [167488] = "@VS Story Cleon vs Secret/Defeat Secret as Cleon",
    [167489] = "@VS Story Cleon vs Madam Luna/Defeat Madam Luna as Cleon",
    [167490] = "@VS Story Cleon vs Dreg/Defeat Dreg as Cleon",
    [167491] = "@VS Story G vs Bob/Defeat Bob as G",
    [167492] = "@VS Story G vs Alkanet/Defeat Alkanet as G",
    [167493] = "@VS Story G vs Marino/Defeat Marino as G",
    [167494] = "@VS Story G vs Kurol/Defeat Kurol as G",
    [167495] = "@VS Story G vs Tamtam/Defeat Tamtam as G",
    [167496] = "@VS Story G vs Cleon/Defeat Cleon as G",
    [167497] = "@VS Story G vs G/Defeat G as G",
    [167498] = "@VS Story G vs Bramb/Defeat Bramb as G",
    [167499] = "@VS Story G vs Develon/Defeat Develon as G",
    [167500] = "@VS Story G vs Gigant/Defeat Gigant as G",
    [167501] = "@VS Story G vs Secret/Defeat Secret as G",
    [167502] = "@VS Story G vs Madam Luna/Defeat Madam Luna as G",
    [167503] = "@VS Story G vs Dreg/Defeat Dreg as G",
    [167504] = "@VS Story Bramb vs Bob/Defeat Bob as Bramb",
    [167505] = "@VS Story Bramb vs Alkanet/Defeat Alkanet as Bramb",
    [167506] = "@VS Story Bramb vs Marino/Defeat Marino as Bramb",
    [167507] = "@VS Story Bramb vs Kurol/Defeat Kurol as Bramb",
    [167508] = "@VS Story Bramb vs Tamtam/Defeat Tamtam as Bramb",
    [167509] = "@VS Story Bramb vs Cleon/Defeat Cleon as Bramb",
    [167510] = "@VS Story Bramb vs G/Defeat G as Bramb",
    [167511] = "@VS Story Bramb vs Bramb/Defeat Bramb as Bramb",
    [167512] = "@VS Story Bramb vs Develon/Defeat Develon as Bramb",
    [167513] = "@VS Story Bramb vs Gigant/Defeat Gigant as Bramb",
    [167514] = "@VS Story Bramb vs Secret/Defeat Secret as Bramb",
    [167515] = "@VS Story Bramb vs Madam Luna/Defeat Madam Luna as Bramb",
    [167516] = "@VS Story Bramb vs Dreg/Defeat Dreg as Bramb",
    [167517] = "@VS Story Develon vs Bob/Defeat Bob as Develon",
    [167518] = "@VS Story Develon vs Alkanet/Defeat Alkanet as Develon",
    [167519] = "@VS Story Develon vs Marino/Defeat Marino as Develon",
    [167520] = "@VS Story Develon vs Kurol/Defeat Kurol as Develon",
    [167521] = "@VS Story Develon vs Tamtam/Defeat Tamtam as Develon",
    [167522] = "@VS Story Develon vs Cleon/Defeat Cleon as Develon",
    [167523] = "@VS Story Develon vs G/Defeat G as Develon",
    [167524] = "@VS Story Develon vs Bramb/Defeat Bramb as Develon",
    [167525] = "@VS Story Develon vs Develon/Defeat Develon as Develon",
    [167526] = "@VS Story Develon vs Gigant/Defeat Gigant as Develon",
    [167527] = "@VS Story Develon vs Secret/Defeat Secret as Develon",
    [167528] = "@VS Story Develon vs Madam Luna/Defeat Madam Luna as Develon",
    [167529] = "@VS Story Develon vs Dreg/Defeat Dreg as Develon",
    [167530] = "@VS Story Gigant vs Bob/Defeat Bob as Gigant",
    [167531] = "@VS Story Gigant vs Alkanet/Defeat Alkanet as Gigant",
    [167532] = "@VS Story Gigant vs Marino/Defeat Marino as Gigant",
    [167533] = "@VS Story Gigant vs Kurol/Defeat Kurol as Gigant",
    [167534] = "@VS Story Gigant vs Tamtam/Defeat Tamtam as Gigant",
    [167535] = "@VS Story Gigant vs Cleon/Defeat Cleon as Gigant",
    [167536] = "@VS Story Gigant vs G/Defeat G as Gigant",
    [167537] = "@VS Story Gigant vs Bramb/Defeat Bramb as Gigant",
    [167538] = "@VS Story Gigant vs Develon/Defeat Develon as Gigant",
    [167539] = "@VS Story Gigant vs Gigant/Defeat Gigant as Gigant",
    [167540] = "@VS Story Gigant vs Secret/Defeat Secret as Gigant",
    [167541] = "@VS Story Gigant vs Madam Luna/Defeat Madam Luna as Gigant",
    [167542] = "@VS Story Gigant vs Dreg/Defeat Dreg as Gigant",
    [167543] = "@VS Story Madam Luna vs Bob/Defeat Bob as Madam Luna",
    [167544] = "@VS Story Madam Luna vs Alkanet/Defeat Alkanet as Madam Luna",
    [167545] = "@VS Story Madam Luna vs Marino/Defeat Marino as Madam Luna",
    [167546] = "@VS Story Madam Luna vs Kurol/Defeat Kurol as Madam Luna",
    [167547] = "@VS Story Madam Luna vs Tamtam/Defeat Tamtam as Madam Luna",
    [167548] = "@VS Story Madam Luna vs Cleon/Defeat Cleon as Madam Luna",
    [167549] = "@VS Story Madam Luna vs G/Defeat G as Madam Luna",
    [167550] = "@VS Story Madam Luna vs Bramb/Defeat Bramb as Madam Luna",
    [167551] = "@VS Story Madam Luna vs Develon/Defeat Develon as Madam Luna",
    [167552] = "@VS Story Madam Luna vs Gigant/Defeat Gigant as Madam Luna",
    [167553] = "@VS Story Madam Luna vs Secret/Defeat Secret as Madam Luna",
    [167554] = "@VS Story Madam Luna vs Madam Luna/Defeat Madam Luna as Madam Luna",
    [167555] = "@VS Story Madam Luna vs Dreg/Defeat Dreg as Madam Luna",
    [167556] = "@VS Story Dreg vs Bob/Defeat Bob as Dreg",
    [167557] = "@VS Story Dreg vs Alkanet/Defeat Alkanet as Dreg",
    [167558] = "@VS Story Dreg vs Marino/Defeat Marino as Dreg",
    [167559] = "@VS Story Dreg vs Kurol/Defeat Kurol as Dreg",
    [167560] = "@VS Story Dreg vs Tamtam/Defeat Tamtam as Dreg",
    [167561] = "@VS Story Dreg vs Cleon/Defeat Cleon as Dreg",
    [167562] = "@VS Story Dreg vs G/Defeat G as Dreg",
    [167563] = "@VS Story Dreg vs Bramb/Defeat Bramb as Dreg",
    [167564] = "@VS Story Dreg vs Develon/Defeat Develon as Dreg",
    [167565] = "@VS Story Dreg vs Gigant/Defeat Gigant as Dreg",
    [167566] = "@VS Story Dreg vs Secret/Defeat Secret as Dreg",
    [167567] = "@VS Story Dreg vs Madam Luna/Defeat Madam Luna as Dreg",
    [167568] = "@VS Story Dreg vs Dreg/Defeat Dreg as Dreg",
    [167569] = "@VS Story Woolen vs Bob/Defeat Bob as Woolen",
    [167570] = "@VS Story Woolen vs Alkanet/Defeat Alkanet as Woolen",
    [167571] = "@VS Story Woolen vs Marino/Defeat Marino as Woolen",
    [167572] = "@VS Story Woolen vs Kurol/Defeat Kurol as Woolen",
    [167573] = "@VS Story Woolen vs Tamtam/Defeat Tamtam as Woolen",
    [167574] = "@VS Story Woolen vs Cleon/Defeat Cleon as Woolen",
    [167575] = "@VS Story Woolen vs G/Defeat G as Woolen",
    [167576] = "@VS Story Woolen vs Bramb/Defeat Bramb as Woolen",
    [167577] = "@VS Story Woolen vs Develon/Defeat Develon as Woolen",
    [167578] = "@VS Story Woolen vs Gigant/Defeat Gigant as Woolen",
    [167579] = "@VS Story Woolen vs Secret/Defeat Secret as Woolen",
    [167580] = "@VS Story Woolen vs Madam Luna/Defeat Madam Luna as Woolen",
    [167581] = "@VS Story Woolen vs Dreg/Defeat Dreg as Woolen",
    [167582] = "@VS Story Packy vs Bob/Defeat Bob as Packy",
    [167583] = "@VS Story Packy vs Alkanet/Defeat Alkanet as Packy",
    [167584] = "@VS Story Packy vs Marino/Defeat Marino as Packy",
    [167585] = "@VS Story Packy vs Kurol/Defeat Kurol as Packy",
    [167586] = "@VS Story Packy vs Tamtam/Defeat Tamtam as Packy",
    [167587] = "@VS Story Packy vs Cleon/Defeat Cleon as Packy",
    [167588] = "@VS Story Packy vs G/Defeat G as Packy",
    [167589] = "@VS Story Packy vs Bramb/Defeat Bramb as Packy",
    [167590] = "@VS Story Packy vs Develon/Defeat Develon as Packy",
    [167591] = "@VS Story Packy vs Gigant/Defeat Gigant as Packy",
    [167592] = "@VS Story Packy vs Secret/Defeat Secret as Packy",
    [167593] = "@VS Story Packy vs Madam Luna/Defeat Madam Luna as Packy",
    [167594] = "@VS Story Packy vs Dreg/Defeat Dreg as Packy",
    [167595] = "@VS Story Monsta vs Bob/Defeat Bob as Monsta",
    [167596] = "@VS Story Monsta vs Alkanet/Defeat Alkanet as Monsta",
    [167597] = "@VS Story Monsta vs Marino/Defeat Marino as Monsta",
    [167598] = "@VS Story Monsta vs Kurol/Defeat Kurol as Monsta",
    [167599] = "@VS Story Monsta vs Tamtam/Defeat Tamtam as Monsta",
    [167600] = "@VS Story Monsta vs Cleon/Defeat Cleon as Monsta",
    [167601] = "@VS Story Monsta vs G/Defeat G as Monsta",
    [167602] = "@VS Story Monsta vs Bramb/Defeat Bramb as Monsta",
    [167603] = "@VS Story Monsta vs Develon/Defeat Develon as Monsta",
    [167604] = "@VS Story Monsta vs Gigant/Defeat Gigant as Monsta",
    [167605] = "@VS Story Monsta vs Secret/Defeat Secret as Monsta",
    [167606] = "@VS Story Monsta vs Madam Luna/Defeat Madam Luna as Monsta",
    [167607] = "@VS Story Monsta vs Dreg/Defeat Dreg as Monsta",
    [167608] = "@VS Story Maita vs Bob/Defeat Bob as Maita",
    [167609] = "@VS Story Maita vs Alkanet/Defeat Alkanet as Maita",
    [167610] = "@VS Story Maita vs Marino/Defeat Marino as Maita",
    [167611] = "@VS Story Maita vs Kurol/Defeat Kurol as Maita",
    [167612] = "@VS Story Maita vs Tamtam/Defeat Tamtam as Maita",
    [167613] = "@VS Story Maita vs Cleon/Defeat Cleon as Maita",
    [167614] = "@VS Story Maita vs G/Defeat G as Maita",
    [167615] = "@VS Story Maita vs Bramb/Defeat Bramb as Maita",
    [167616] = "@VS Story Maita vs Develon/Defeat Develon as Maita",
    [167617] = "@VS Story Maita vs Gigant/Defeat Gigant as Maita",
    [167618] = "@VS Story Maita vs Secret/Defeat Secret as Maita",
    [167619] = "@VS Story Maita vs Madam Luna/Defeat Madam Luna as Maita",
    [167620] = "@VS Story Maita vs Dreg/Defeat Dreg as Maita",
}

local item_map = {
    [200020] = "Unlock C world",
    [200021] = "Unlock D world",
    [200022] = "Unlock E world",
    [200023] = "Unlock F world",
    [200024] = "Unlock G world",
    [200025] = "Unlock H world",
    [200026] = "Unlock I world",
    [200027] = "Unlock J world",
    [200028] = "Unlock K world",
    [200029] = "Unlock L world",
    [200030] = "Unlock M world",
    [200031] = "Unlock N world",
    [200032] = "Unlock O world",
    [200033] = "Unlock P world",
    [200034] = "Unlock Q world",
    [200035] = "Unlock R world",
    [200036] = "Unlock S world",
    [200037] = "Unlock T world",
    [200038] = "Unlock U world",
    [200039] = "Unlock V world",
    [200040] = "Unlock W world",
    [200041] = "Unlock ? world",
    [200042] = "Unlock X world",
    [200043] = "Unlock Y world",
    [200044] = "Unlock Z world",
    [200100] = "Unlock Tarot Star",
    [200101] = "Unlock Tarot Moon",
    [200102] = "Unlock Tarot Hermit",
    [200103] = "Unlock Tarot Tower",
    [200104] = "Unlock Tarot Chariot",
    [200105] = "Unlock Tarot Death",
    [200106] = "Unlock Tarot Devil",
    [200107] = "Unlock Tarot Hanged Man",
    [200108] = "Unlock Tarot Magician",
    [200109] = "Unlock Tarot Wheel of Fortune",
    [200110] = "Unlock Tarot Judgement",
    [200111] = "Unlock Tarot Lovers",
    [200112] = "Unlock Tarot Temperance",
    [200113] = "Unlock Tarot Hierophant",
    [200114] = "Unlock Tarot Emperor",
    [200115] = "Unlock Tarot Empress",
    [200116] = "Unlock Tarot High Priestess",
    [200117] = "Unlock Tarot Sun",
    [200118] = "Unlock Tarot Justice",
    [200119] = "Unlock Tarot Strength",
    [200120] = "Unlock Tarot World",
    [200200] = "Unlock VS/Story Character Bob",
    [200201] = "Unlock VS/Story Character Alkanet",
    [200202] = "Unlock VS/Story Character Marino",
    [200203] = "Unlock VS/Story Character Kurol",
    [200204] = "Unlock VS/Story Character Tamtam",
    [200205] = "Unlock VS/Story Character Cleon",
    [200206] = "Unlock VS/Story Character G",
    [200207] = "Unlock VS/Story Character Bramb",
    [200208] = "Unlock VS/Story Character Develon",
    [200209] = "Unlock VS/Story Character Gigant",
    [200210] = "Unlock VS/Story Character Madam Luna",
    [200211] = "Unlock VS/Story Character Dreg",
    [200212] = "Unlock VS/Story Character Woolen",
    [200213] = "Unlock VS/Story Character Packy",
    [200214] = "Unlock VS/Story Character Monsta",
    [200215] = "Unlock VS/Story Character Maita",
    [200246] = "Unlock VS/Story",
    [200247] = "Unlock VS/Contest",
    [200220] = "Progressive Enemy",
}


-- VS/Story Complete checks (16 player characters x 13 opponents).
local vs_story_complete_location_map = {
    [167413] = "@VS Story Bob vs Bob/Defeat Bob as Bob",
    [167414] = "@VS Story Bob vs Alkanet/Defeat Alkanet as Bob",
    [167415] = "@VS Story Bob vs Marino/Defeat Marino as Bob",
    [167416] = "@VS Story Bob vs Kurol/Defeat Kurol as Bob",
    [167417] = "@VS Story Bob vs Tamtam/Defeat Tamtam as Bob",
    [167418] = "@VS Story Bob vs Cleon/Defeat Cleon as Bob",
    [167419] = "@VS Story Bob vs G/Defeat G as Bob",
    [167420] = "@VS Story Bob vs Bramb/Defeat Bramb as Bob",
    [167421] = "@VS Story Bob vs Develon/Defeat Develon as Bob",
    [167422] = "@VS Story Bob vs Gigant/Defeat Gigant as Bob",
    [167423] = "@VS Story Bob vs Secret/Defeat Secret as Bob",
    [167424] = "@VS Story Bob vs Madam Luna/Defeat Madam Luna as Bob",
    [167425] = "@VS Story Bob vs Dreg/Defeat Dreg as Bob",
    [167426] = "@VS Story Alkanet vs Bob/Defeat Bob as Alkanet",
    [167427] = "@VS Story Alkanet vs Alkanet/Defeat Alkanet as Alkanet",
    [167428] = "@VS Story Alkanet vs Marino/Defeat Marino as Alkanet",
    [167429] = "@VS Story Alkanet vs Kurol/Defeat Kurol as Alkanet",
    [167430] = "@VS Story Alkanet vs Tamtam/Defeat Tamtam as Alkanet",
    [167431] = "@VS Story Alkanet vs Cleon/Defeat Cleon as Alkanet",
    [167432] = "@VS Story Alkanet vs G/Defeat G as Alkanet",
    [167433] = "@VS Story Alkanet vs Bramb/Defeat Bramb as Alkanet",
    [167434] = "@VS Story Alkanet vs Develon/Defeat Develon as Alkanet",
    [167435] = "@VS Story Alkanet vs Gigant/Defeat Gigant as Alkanet",
    [167436] = "@VS Story Alkanet vs Secret/Defeat Secret as Alkanet",
    [167437] = "@VS Story Alkanet vs Madam Luna/Defeat Madam Luna as Alkanet",
    [167438] = "@VS Story Alkanet vs Dreg/Defeat Dreg as Alkanet",
    [167439] = "@VS Story Marino vs Bob/Defeat Bob as Marino",
    [167440] = "@VS Story Marino vs Alkanet/Defeat Alkanet as Marino",
    [167441] = "@VS Story Marino vs Marino/Defeat Marino as Marino",
    [167442] = "@VS Story Marino vs Kurol/Defeat Kurol as Marino",
    [167443] = "@VS Story Marino vs Tamtam/Defeat Tamtam as Marino",
    [167444] = "@VS Story Marino vs Cleon/Defeat Cleon as Marino",
    [167445] = "@VS Story Marino vs G/Defeat G as Marino",
    [167446] = "@VS Story Marino vs Bramb/Defeat Bramb as Marino",
    [167447] = "@VS Story Marino vs Develon/Defeat Develon as Marino",
    [167448] = "@VS Story Marino vs Gigant/Defeat Gigant as Marino",
    [167449] = "@VS Story Marino vs Secret/Defeat Secret as Marino",
    [167450] = "@VS Story Marino vs Madam Luna/Defeat Madam Luna as Marino",
    [167451] = "@VS Story Marino vs Dreg/Defeat Dreg as Marino",
    [167452] = "@VS Story Kurol vs Bob/Defeat Bob as Kurol",
    [167453] = "@VS Story Kurol vs Alkanet/Defeat Alkanet as Kurol",
    [167454] = "@VS Story Kurol vs Marino/Defeat Marino as Kurol",
    [167455] = "@VS Story Kurol vs Kurol/Defeat Kurol as Kurol",
    [167456] = "@VS Story Kurol vs Tamtam/Defeat Tamtam as Kurol",
    [167457] = "@VS Story Kurol vs Cleon/Defeat Cleon as Kurol",
    [167458] = "@VS Story Kurol vs G/Defeat G as Kurol",
    [167459] = "@VS Story Kurol vs Bramb/Defeat Bramb as Kurol",
    [167460] = "@VS Story Kurol vs Develon/Defeat Develon as Kurol",
    [167461] = "@VS Story Kurol vs Gigant/Defeat Gigant as Kurol",
    [167462] = "@VS Story Kurol vs Secret/Defeat Secret as Kurol",
    [167463] = "@VS Story Kurol vs Madam Luna/Defeat Madam Luna as Kurol",
    [167464] = "@VS Story Kurol vs Dreg/Defeat Dreg as Kurol",
    [167465] = "@VS Story Tamtam vs Bob/Defeat Bob as Tamtam",
    [167466] = "@VS Story Tamtam vs Alkanet/Defeat Alkanet as Tamtam",
    [167467] = "@VS Story Tamtam vs Marino/Defeat Marino as Tamtam",
    [167468] = "@VS Story Tamtam vs Kurol/Defeat Kurol as Tamtam",
    [167469] = "@VS Story Tamtam vs Tamtam/Defeat Tamtam as Tamtam",
    [167470] = "@VS Story Tamtam vs Cleon/Defeat Cleon as Tamtam",
    [167471] = "@VS Story Tamtam vs G/Defeat G as Tamtam",
    [167472] = "@VS Story Tamtam vs Bramb/Defeat Bramb as Tamtam",
    [167473] = "@VS Story Tamtam vs Develon/Defeat Develon as Tamtam",
    [167474] = "@VS Story Tamtam vs Gigant/Defeat Gigant as Tamtam",
    [167475] = "@VS Story Tamtam vs Secret/Defeat Secret as Tamtam",
    [167476] = "@VS Story Tamtam vs Madam Luna/Defeat Madam Luna as Tamtam",
    [167477] = "@VS Story Tamtam vs Dreg/Defeat Dreg as Tamtam",
    [167478] = "@VS Story Cleon vs Bob/Defeat Bob as Cleon",
    [167479] = "@VS Story Cleon vs Alkanet/Defeat Alkanet as Cleon",
    [167480] = "@VS Story Cleon vs Marino/Defeat Marino as Cleon",
    [167481] = "@VS Story Cleon vs Kurol/Defeat Kurol as Cleon",
    [167482] = "@VS Story Cleon vs Tamtam/Defeat Tamtam as Cleon",
    [167483] = "@VS Story Cleon vs Cleon/Defeat Cleon as Cleon",
    [167484] = "@VS Story Cleon vs G/Defeat G as Cleon",
    [167485] = "@VS Story Cleon vs Bramb/Defeat Bramb as Cleon",
    [167486] = "@VS Story Cleon vs Develon/Defeat Develon as Cleon",
    [167487] = "@VS Story Cleon vs Gigant/Defeat Gigant as Cleon",
    [167488] = "@VS Story Cleon vs Secret/Defeat Secret as Cleon",
    [167489] = "@VS Story Cleon vs Madam Luna/Defeat Madam Luna as Cleon",
    [167490] = "@VS Story Cleon vs Dreg/Defeat Dreg as Cleon",
    [167491] = "@VS Story G vs Bob/Defeat Bob as G",
    [167492] = "@VS Story G vs Alkanet/Defeat Alkanet as G",
    [167493] = "@VS Story G vs Marino/Defeat Marino as G",
    [167494] = "@VS Story G vs Kurol/Defeat Kurol as G",
    [167495] = "@VS Story G vs Tamtam/Defeat Tamtam as G",
    [167496] = "@VS Story G vs Cleon/Defeat Cleon as G",
    [167497] = "@VS Story G vs G/Defeat G as G",
    [167498] = "@VS Story G vs Bramb/Defeat Bramb as G",
    [167499] = "@VS Story G vs Develon/Defeat Develon as G",
    [167500] = "@VS Story G vs Gigant/Defeat Gigant as G",
    [167501] = "@VS Story G vs Secret/Defeat Secret as G",
    [167502] = "@VS Story G vs Madam Luna/Defeat Madam Luna as G",
    [167503] = "@VS Story G vs Dreg/Defeat Dreg as G",
    [167504] = "@VS Story Bramb vs Bob/Defeat Bob as Bramb",
    [167505] = "@VS Story Bramb vs Alkanet/Defeat Alkanet as Bramb",
    [167506] = "@VS Story Bramb vs Marino/Defeat Marino as Bramb",
    [167507] = "@VS Story Bramb vs Kurol/Defeat Kurol as Bramb",
    [167508] = "@VS Story Bramb vs Tamtam/Defeat Tamtam as Bramb",
    [167509] = "@VS Story Bramb vs Cleon/Defeat Cleon as Bramb",
    [167510] = "@VS Story Bramb vs G/Defeat G as Bramb",
    [167511] = "@VS Story Bramb vs Bramb/Defeat Bramb as Bramb",
    [167512] = "@VS Story Bramb vs Develon/Defeat Develon as Bramb",
    [167513] = "@VS Story Bramb vs Gigant/Defeat Gigant as Bramb",
    [167514] = "@VS Story Bramb vs Secret/Defeat Secret as Bramb",
    [167515] = "@VS Story Bramb vs Madam Luna/Defeat Madam Luna as Bramb",
    [167516] = "@VS Story Bramb vs Dreg/Defeat Dreg as Bramb",
    [167517] = "@VS Story Develon vs Bob/Defeat Bob as Develon",
    [167518] = "@VS Story Develon vs Alkanet/Defeat Alkanet as Develon",
    [167519] = "@VS Story Develon vs Marino/Defeat Marino as Develon",
    [167520] = "@VS Story Develon vs Kurol/Defeat Kurol as Develon",
    [167521] = "@VS Story Develon vs Tamtam/Defeat Tamtam as Develon",
    [167522] = "@VS Story Develon vs Cleon/Defeat Cleon as Develon",
    [167523] = "@VS Story Develon vs G/Defeat G as Develon",
    [167524] = "@VS Story Develon vs Bramb/Defeat Bramb as Develon",
    [167525] = "@VS Story Develon vs Develon/Defeat Develon as Develon",
    [167526] = "@VS Story Develon vs Gigant/Defeat Gigant as Develon",
    [167527] = "@VS Story Develon vs Secret/Defeat Secret as Develon",
    [167528] = "@VS Story Develon vs Madam Luna/Defeat Madam Luna as Develon",
    [167529] = "@VS Story Develon vs Dreg/Defeat Dreg as Develon",
    [167530] = "@VS Story Gigant vs Bob/Defeat Bob as Gigant",
    [167531] = "@VS Story Gigant vs Alkanet/Defeat Alkanet as Gigant",
    [167532] = "@VS Story Gigant vs Marino/Defeat Marino as Gigant",
    [167533] = "@VS Story Gigant vs Kurol/Defeat Kurol as Gigant",
    [167534] = "@VS Story Gigant vs Tamtam/Defeat Tamtam as Gigant",
    [167535] = "@VS Story Gigant vs Cleon/Defeat Cleon as Gigant",
    [167536] = "@VS Story Gigant vs G/Defeat G as Gigant",
    [167537] = "@VS Story Gigant vs Bramb/Defeat Bramb as Gigant",
    [167538] = "@VS Story Gigant vs Develon/Defeat Develon as Gigant",
    [167539] = "@VS Story Gigant vs Gigant/Defeat Gigant as Gigant",
    [167540] = "@VS Story Gigant vs Secret/Defeat Secret as Gigant",
    [167541] = "@VS Story Gigant vs Madam Luna/Defeat Madam Luna as Gigant",
    [167542] = "@VS Story Gigant vs Dreg/Defeat Dreg as Gigant",
    [167543] = "@VS Story Madam Luna vs Bob/Defeat Bob as Madam Luna",
    [167544] = "@VS Story Madam Luna vs Alkanet/Defeat Alkanet as Madam Luna",
    [167545] = "@VS Story Madam Luna vs Marino/Defeat Marino as Madam Luna",
    [167546] = "@VS Story Madam Luna vs Kurol/Defeat Kurol as Madam Luna",
    [167547] = "@VS Story Madam Luna vs Tamtam/Defeat Tamtam as Madam Luna",
    [167548] = "@VS Story Madam Luna vs Cleon/Defeat Cleon as Madam Luna",
    [167549] = "@VS Story Madam Luna vs G/Defeat G as Madam Luna",
    [167550] = "@VS Story Madam Luna vs Bramb/Defeat Bramb as Madam Luna",
    [167551] = "@VS Story Madam Luna vs Develon/Defeat Develon as Madam Luna",
    [167552] = "@VS Story Madam Luna vs Gigant/Defeat Gigant as Madam Luna",
    [167553] = "@VS Story Madam Luna vs Secret/Defeat Secret as Madam Luna",
    [167554] = "@VS Story Madam Luna vs Madam Luna/Defeat Madam Luna as Madam Luna",
    [167555] = "@VS Story Madam Luna vs Dreg/Defeat Dreg as Madam Luna",
    [167556] = "@VS Story Dreg vs Bob/Defeat Bob as Dreg",
    [167557] = "@VS Story Dreg vs Alkanet/Defeat Alkanet as Dreg",
    [167558] = "@VS Story Dreg vs Marino/Defeat Marino as Dreg",
    [167559] = "@VS Story Dreg vs Kurol/Defeat Kurol as Dreg",
    [167560] = "@VS Story Dreg vs Tamtam/Defeat Tamtam as Dreg",
    [167561] = "@VS Story Dreg vs Cleon/Defeat Cleon as Dreg",
    [167562] = "@VS Story Dreg vs G/Defeat G as Dreg",
    [167563] = "@VS Story Dreg vs Bramb/Defeat Bramb as Dreg",
    [167564] = "@VS Story Dreg vs Develon/Defeat Develon as Dreg",
    [167565] = "@VS Story Dreg vs Gigant/Defeat Gigant as Dreg",
    [167566] = "@VS Story Dreg vs Secret/Defeat Secret as Dreg",
    [167567] = "@VS Story Dreg vs Madam Luna/Defeat Madam Luna as Dreg",
    [167568] = "@VS Story Dreg vs Dreg/Defeat Dreg as Dreg",
    [167569] = "@VS Story Woolen vs Bob/Defeat Bob as Woolen",
    [167570] = "@VS Story Woolen vs Alkanet/Defeat Alkanet as Woolen",
    [167571] = "@VS Story Woolen vs Marino/Defeat Marino as Woolen",
    [167572] = "@VS Story Woolen vs Kurol/Defeat Kurol as Woolen",
    [167573] = "@VS Story Woolen vs Tamtam/Defeat Tamtam as Woolen",
    [167574] = "@VS Story Woolen vs Cleon/Defeat Cleon as Woolen",
    [167575] = "@VS Story Woolen vs G/Defeat G as Woolen",
    [167576] = "@VS Story Woolen vs Bramb/Defeat Bramb as Woolen",
    [167577] = "@VS Story Woolen vs Develon/Defeat Develon as Woolen",
    [167578] = "@VS Story Woolen vs Gigant/Defeat Gigant as Woolen",
    [167579] = "@VS Story Woolen vs Secret/Defeat Secret as Woolen",
    [167580] = "@VS Story Woolen vs Madam Luna/Defeat Madam Luna as Woolen",
    [167581] = "@VS Story Woolen vs Dreg/Defeat Dreg as Woolen",
    [167582] = "@VS Story Packy vs Bob/Defeat Bob as Packy",
    [167583] = "@VS Story Packy vs Alkanet/Defeat Alkanet as Packy",
    [167584] = "@VS Story Packy vs Marino/Defeat Marino as Packy",
    [167585] = "@VS Story Packy vs Kurol/Defeat Kurol as Packy",
    [167586] = "@VS Story Packy vs Tamtam/Defeat Tamtam as Packy",
    [167587] = "@VS Story Packy vs Cleon/Defeat Cleon as Packy",
    [167588] = "@VS Story Packy vs G/Defeat G as Packy",
    [167589] = "@VS Story Packy vs Bramb/Defeat Bramb as Packy",
    [167590] = "@VS Story Packy vs Develon/Defeat Develon as Packy",
    [167591] = "@VS Story Packy vs Gigant/Defeat Gigant as Packy",
    [167592] = "@VS Story Packy vs Secret/Defeat Secret as Packy",
    [167593] = "@VS Story Packy vs Madam Luna/Defeat Madam Luna as Packy",
    [167594] = "@VS Story Packy vs Dreg/Defeat Dreg as Packy",
    [167595] = "@VS Story Monsta vs Bob/Defeat Bob as Monsta",
    [167596] = "@VS Story Monsta vs Alkanet/Defeat Alkanet as Monsta",
    [167597] = "@VS Story Monsta vs Marino/Defeat Marino as Monsta",
    [167598] = "@VS Story Monsta vs Kurol/Defeat Kurol as Monsta",
    [167599] = "@VS Story Monsta vs Tamtam/Defeat Tamtam as Monsta",
    [167600] = "@VS Story Monsta vs Cleon/Defeat Cleon as Monsta",
    [167601] = "@VS Story Monsta vs G/Defeat G as Monsta",
    [167602] = "@VS Story Monsta vs Bramb/Defeat Bramb as Monsta",
    [167603] = "@VS Story Monsta vs Develon/Defeat Develon as Monsta",
    [167604] = "@VS Story Monsta vs Gigant/Defeat Gigant as Monsta",
    [167605] = "@VS Story Monsta vs Secret/Defeat Secret as Monsta",
    [167606] = "@VS Story Monsta vs Madam Luna/Defeat Madam Luna as Monsta",
    [167607] = "@VS Story Monsta vs Dreg/Defeat Dreg as Monsta",
    [167608] = "@VS Story Maita vs Bob/Defeat Bob as Maita",
    [167609] = "@VS Story Maita vs Alkanet/Defeat Alkanet as Maita",
    [167610] = "@VS Story Maita vs Marino/Defeat Marino as Maita",
    [167611] = "@VS Story Maita vs Kurol/Defeat Kurol as Maita",
    [167612] = "@VS Story Maita vs Tamtam/Defeat Tamtam as Maita",
    [167613] = "@VS Story Maita vs Cleon/Defeat Cleon as Maita",
    [167614] = "@VS Story Maita vs G/Defeat G as Maita",
    [167615] = "@VS Story Maita vs Bramb/Defeat Bramb as Maita",
    [167616] = "@VS Story Maita vs Develon/Defeat Develon as Maita",
    [167617] = "@VS Story Maita vs Gigant/Defeat Gigant as Maita",
    [167618] = "@VS Story Maita vs Secret/Defeat Secret as Maita",
    [167619] = "@VS Story Maita vs Madam Luna/Defeat Madam Luna as Maita",
    [167620] = "@VS Story Maita vs Dreg/Defeat Dreg as Maita",
}

-- VS/Contest Complete checks (16 characters x up to 30 wins).
local vs_contest_complete_location_map = {
    [168030] = "@VS Contest Bub/Win Contest 1 as Bub",
    [168031] = "@VS Contest Bub/Win Contest 2 as Bub",
    [168032] = "@VS Contest Bub/Win Contest 3 as Bub",
    [168033] = "@VS Contest Bub/Win Contest 4 as Bub",
    [168034] = "@VS Contest Bub/Win Contest 5 as Bub",
    [168035] = "@VS Contest Bub/Win Contest 6 as Bub",
    [168036] = "@VS Contest Bub/Win Contest 7 as Bub",
    [168037] = "@VS Contest Bub/Win Contest 8 as Bub",
    [168038] = "@VS Contest Bub/Win Contest 9 as Bub",
    [168039] = "@VS Contest Bub/Win Contest 10 as Bub",
    [168040] = "@VS Contest Bub/Win Contest 11 as Bub",
    [168041] = "@VS Contest Bub/Win Contest 12 as Bub",
    [168042] = "@VS Contest Bub/Win Contest 13 as Bub",
    [168043] = "@VS Contest Bub/Win Contest 14 as Bub",
    [168044] = "@VS Contest Bub/Win Contest 15 as Bub",
    [168045] = "@VS Contest Bub/Win Contest 16 as Bub",
    [168046] = "@VS Contest Bub/Win Contest 17 as Bub",
    [168047] = "@VS Contest Bub/Win Contest 18 as Bub",
    [168048] = "@VS Contest Bub/Win Contest 19 as Bub",
    [168049] = "@VS Contest Bub/Win Contest 20 as Bub",
    [168050] = "@VS Contest Bub/Win Contest 21 as Bub",
    [168051] = "@VS Contest Bub/Win Contest 22 as Bub",
    [168052] = "@VS Contest Bub/Win Contest 23 as Bub",
    [168053] = "@VS Contest Bub/Win Contest 24 as Bub",
    [168054] = "@VS Contest Bub/Win Contest 25 as Bub",
    [168055] = "@VS Contest Bub/Win Contest 26 as Bub",
    [168056] = "@VS Contest Bub/Win Contest 27 as Bub",
    [168057] = "@VS Contest Bub/Win Contest 28 as Bub",
    [168058] = "@VS Contest Bub/Win Contest 29 as Bub",
    [168059] = "@VS Contest Bub/Win Contest 30 as Bub",
    [168060] = "@VS Contest Alkanet/Win Contest 1 as Alkanet",
    [168061] = "@VS Contest Alkanet/Win Contest 2 as Alkanet",
    [168062] = "@VS Contest Alkanet/Win Contest 3 as Alkanet",
    [168063] = "@VS Contest Alkanet/Win Contest 4 as Alkanet",
    [168064] = "@VS Contest Alkanet/Win Contest 5 as Alkanet",
    [168065] = "@VS Contest Alkanet/Win Contest 6 as Alkanet",
    [168066] = "@VS Contest Alkanet/Win Contest 7 as Alkanet",
    [168067] = "@VS Contest Alkanet/Win Contest 8 as Alkanet",
    [168068] = "@VS Contest Alkanet/Win Contest 9 as Alkanet",
    [168069] = "@VS Contest Alkanet/Win Contest 10 as Alkanet",
    [168070] = "@VS Contest Alkanet/Win Contest 11 as Alkanet",
    [168071] = "@VS Contest Alkanet/Win Contest 12 as Alkanet",
    [168072] = "@VS Contest Alkanet/Win Contest 13 as Alkanet",
    [168073] = "@VS Contest Alkanet/Win Contest 14 as Alkanet",
    [168074] = "@VS Contest Alkanet/Win Contest 15 as Alkanet",
    [168075] = "@VS Contest Alkanet/Win Contest 16 as Alkanet",
    [168076] = "@VS Contest Alkanet/Win Contest 17 as Alkanet",
    [168077] = "@VS Contest Alkanet/Win Contest 18 as Alkanet",
    [168078] = "@VS Contest Alkanet/Win Contest 19 as Alkanet",
    [168079] = "@VS Contest Alkanet/Win Contest 20 as Alkanet",
    [168080] = "@VS Contest Alkanet/Win Contest 21 as Alkanet",
    [168081] = "@VS Contest Alkanet/Win Contest 22 as Alkanet",
    [168082] = "@VS Contest Alkanet/Win Contest 23 as Alkanet",
    [168083] = "@VS Contest Alkanet/Win Contest 24 as Alkanet",
    [168084] = "@VS Contest Alkanet/Win Contest 25 as Alkanet",
    [168085] = "@VS Contest Alkanet/Win Contest 26 as Alkanet",
    [168086] = "@VS Contest Alkanet/Win Contest 27 as Alkanet",
    [168087] = "@VS Contest Alkanet/Win Contest 28 as Alkanet",
    [168088] = "@VS Contest Alkanet/Win Contest 29 as Alkanet",
    [168089] = "@VS Contest Alkanet/Win Contest 30 as Alkanet",
    [168090] = "@VS Contest Marino/Win Contest 1 as Marino",
    [168091] = "@VS Contest Marino/Win Contest 2 as Marino",
    [168092] = "@VS Contest Marino/Win Contest 3 as Marino",
    [168093] = "@VS Contest Marino/Win Contest 4 as Marino",
    [168094] = "@VS Contest Marino/Win Contest 5 as Marino",
    [168095] = "@VS Contest Marino/Win Contest 6 as Marino",
    [168096] = "@VS Contest Marino/Win Contest 7 as Marino",
    [168097] = "@VS Contest Marino/Win Contest 8 as Marino",
    [168098] = "@VS Contest Marino/Win Contest 9 as Marino",
    [168099] = "@VS Contest Marino/Win Contest 10 as Marino",
    [168100] = "@VS Contest Marino/Win Contest 11 as Marino",
    [168101] = "@VS Contest Marino/Win Contest 12 as Marino",
    [168102] = "@VS Contest Marino/Win Contest 13 as Marino",
    [168103] = "@VS Contest Marino/Win Contest 14 as Marino",
    [168104] = "@VS Contest Marino/Win Contest 15 as Marino",
    [168105] = "@VS Contest Marino/Win Contest 16 as Marino",
    [168106] = "@VS Contest Marino/Win Contest 17 as Marino",
    [168107] = "@VS Contest Marino/Win Contest 18 as Marino",
    [168108] = "@VS Contest Marino/Win Contest 19 as Marino",
    [168109] = "@VS Contest Marino/Win Contest 20 as Marino",
    [168110] = "@VS Contest Marino/Win Contest 21 as Marino",
    [168111] = "@VS Contest Marino/Win Contest 22 as Marino",
    [168112] = "@VS Contest Marino/Win Contest 23 as Marino",
    [168113] = "@VS Contest Marino/Win Contest 24 as Marino",
    [168114] = "@VS Contest Marino/Win Contest 25 as Marino",
    [168115] = "@VS Contest Marino/Win Contest 26 as Marino",
    [168116] = "@VS Contest Marino/Win Contest 27 as Marino",
    [168117] = "@VS Contest Marino/Win Contest 28 as Marino",
    [168118] = "@VS Contest Marino/Win Contest 29 as Marino",
    [168119] = "@VS Contest Marino/Win Contest 30 as Marino",
    [168120] = "@VS Contest Kurol/Win Contest 1 as Kurol",
    [168121] = "@VS Contest Kurol/Win Contest 2 as Kurol",
    [168122] = "@VS Contest Kurol/Win Contest 3 as Kurol",
    [168123] = "@VS Contest Kurol/Win Contest 4 as Kurol",
    [168124] = "@VS Contest Kurol/Win Contest 5 as Kurol",
    [168125] = "@VS Contest Kurol/Win Contest 6 as Kurol",
    [168126] = "@VS Contest Kurol/Win Contest 7 as Kurol",
    [168127] = "@VS Contest Kurol/Win Contest 8 as Kurol",
    [168128] = "@VS Contest Kurol/Win Contest 9 as Kurol",
    [168129] = "@VS Contest Kurol/Win Contest 10 as Kurol",
    [168130] = "@VS Contest Kurol/Win Contest 11 as Kurol",
    [168131] = "@VS Contest Kurol/Win Contest 12 as Kurol",
    [168132] = "@VS Contest Kurol/Win Contest 13 as Kurol",
    [168133] = "@VS Contest Kurol/Win Contest 14 as Kurol",
    [168134] = "@VS Contest Kurol/Win Contest 15 as Kurol",
    [168135] = "@VS Contest Kurol/Win Contest 16 as Kurol",
    [168136] = "@VS Contest Kurol/Win Contest 17 as Kurol",
    [168137] = "@VS Contest Kurol/Win Contest 18 as Kurol",
    [168138] = "@VS Contest Kurol/Win Contest 19 as Kurol",
    [168139] = "@VS Contest Kurol/Win Contest 20 as Kurol",
    [168140] = "@VS Contest Kurol/Win Contest 21 as Kurol",
    [168141] = "@VS Contest Kurol/Win Contest 22 as Kurol",
    [168142] = "@VS Contest Kurol/Win Contest 23 as Kurol",
    [168143] = "@VS Contest Kurol/Win Contest 24 as Kurol",
    [168144] = "@VS Contest Kurol/Win Contest 25 as Kurol",
    [168145] = "@VS Contest Kurol/Win Contest 26 as Kurol",
    [168146] = "@VS Contest Kurol/Win Contest 27 as Kurol",
    [168147] = "@VS Contest Kurol/Win Contest 28 as Kurol",
    [168148] = "@VS Contest Kurol/Win Contest 29 as Kurol",
    [168149] = "@VS Contest Kurol/Win Contest 30 as Kurol",
    [168150] = "@VS Contest Tamtam/Win Contest 1 as Tamtam",
    [168151] = "@VS Contest Tamtam/Win Contest 2 as Tamtam",
    [168152] = "@VS Contest Tamtam/Win Contest 3 as Tamtam",
    [168153] = "@VS Contest Tamtam/Win Contest 4 as Tamtam",
    [168154] = "@VS Contest Tamtam/Win Contest 5 as Tamtam",
    [168155] = "@VS Contest Tamtam/Win Contest 6 as Tamtam",
    [168156] = "@VS Contest Tamtam/Win Contest 7 as Tamtam",
    [168157] = "@VS Contest Tamtam/Win Contest 8 as Tamtam",
    [168158] = "@VS Contest Tamtam/Win Contest 9 as Tamtam",
    [168159] = "@VS Contest Tamtam/Win Contest 10 as Tamtam",
    [168160] = "@VS Contest Tamtam/Win Contest 11 as Tamtam",
    [168161] = "@VS Contest Tamtam/Win Contest 12 as Tamtam",
    [168162] = "@VS Contest Tamtam/Win Contest 13 as Tamtam",
    [168163] = "@VS Contest Tamtam/Win Contest 14 as Tamtam",
    [168164] = "@VS Contest Tamtam/Win Contest 15 as Tamtam",
    [168165] = "@VS Contest Tamtam/Win Contest 16 as Tamtam",
    [168166] = "@VS Contest Tamtam/Win Contest 17 as Tamtam",
    [168167] = "@VS Contest Tamtam/Win Contest 18 as Tamtam",
    [168168] = "@VS Contest Tamtam/Win Contest 19 as Tamtam",
    [168169] = "@VS Contest Tamtam/Win Contest 20 as Tamtam",
    [168170] = "@VS Contest Tamtam/Win Contest 21 as Tamtam",
    [168171] = "@VS Contest Tamtam/Win Contest 22 as Tamtam",
    [168172] = "@VS Contest Tamtam/Win Contest 23 as Tamtam",
    [168173] = "@VS Contest Tamtam/Win Contest 24 as Tamtam",
    [168174] = "@VS Contest Tamtam/Win Contest 25 as Tamtam",
    [168175] = "@VS Contest Tamtam/Win Contest 26 as Tamtam",
    [168176] = "@VS Contest Tamtam/Win Contest 27 as Tamtam",
    [168177] = "@VS Contest Tamtam/Win Contest 28 as Tamtam",
    [168178] = "@VS Contest Tamtam/Win Contest 29 as Tamtam",
    [168179] = "@VS Contest Tamtam/Win Contest 30 as Tamtam",
    [168180] = "@VS Contest Cleon/Win Contest 1 as Cleon",
    [168181] = "@VS Contest Cleon/Win Contest 2 as Cleon",
    [168182] = "@VS Contest Cleon/Win Contest 3 as Cleon",
    [168183] = "@VS Contest Cleon/Win Contest 4 as Cleon",
    [168184] = "@VS Contest Cleon/Win Contest 5 as Cleon",
    [168185] = "@VS Contest Cleon/Win Contest 6 as Cleon",
    [168186] = "@VS Contest Cleon/Win Contest 7 as Cleon",
    [168187] = "@VS Contest Cleon/Win Contest 8 as Cleon",
    [168188] = "@VS Contest Cleon/Win Contest 9 as Cleon",
    [168189] = "@VS Contest Cleon/Win Contest 10 as Cleon",
    [168190] = "@VS Contest Cleon/Win Contest 11 as Cleon",
    [168191] = "@VS Contest Cleon/Win Contest 12 as Cleon",
    [168192] = "@VS Contest Cleon/Win Contest 13 as Cleon",
    [168193] = "@VS Contest Cleon/Win Contest 14 as Cleon",
    [168194] = "@VS Contest Cleon/Win Contest 15 as Cleon",
    [168195] = "@VS Contest Cleon/Win Contest 16 as Cleon",
    [168196] = "@VS Contest Cleon/Win Contest 17 as Cleon",
    [168197] = "@VS Contest Cleon/Win Contest 18 as Cleon",
    [168198] = "@VS Contest Cleon/Win Contest 19 as Cleon",
    [168199] = "@VS Contest Cleon/Win Contest 20 as Cleon",
    [168200] = "@VS Contest Cleon/Win Contest 21 as Cleon",
    [168201] = "@VS Contest Cleon/Win Contest 22 as Cleon",
    [168202] = "@VS Contest Cleon/Win Contest 23 as Cleon",
    [168203] = "@VS Contest Cleon/Win Contest 24 as Cleon",
    [168204] = "@VS Contest Cleon/Win Contest 25 as Cleon",
    [168205] = "@VS Contest Cleon/Win Contest 26 as Cleon",
    [168206] = "@VS Contest Cleon/Win Contest 27 as Cleon",
    [168207] = "@VS Contest Cleon/Win Contest 28 as Cleon",
    [168208] = "@VS Contest Cleon/Win Contest 29 as Cleon",
    [168209] = "@VS Contest Cleon/Win Contest 30 as Cleon",
    [168210] = "@VS Contest G/Win Contest 1 as G",
    [168211] = "@VS Contest G/Win Contest 2 as G",
    [168212] = "@VS Contest G/Win Contest 3 as G",
    [168213] = "@VS Contest G/Win Contest 4 as G",
    [168214] = "@VS Contest G/Win Contest 5 as G",
    [168215] = "@VS Contest G/Win Contest 6 as G",
    [168216] = "@VS Contest G/Win Contest 7 as G",
    [168217] = "@VS Contest G/Win Contest 8 as G",
    [168218] = "@VS Contest G/Win Contest 9 as G",
    [168219] = "@VS Contest G/Win Contest 10 as G",
    [168220] = "@VS Contest G/Win Contest 11 as G",
    [168221] = "@VS Contest G/Win Contest 12 as G",
    [168222] = "@VS Contest G/Win Contest 13 as G",
    [168223] = "@VS Contest G/Win Contest 14 as G",
    [168224] = "@VS Contest G/Win Contest 15 as G",
    [168225] = "@VS Contest G/Win Contest 16 as G",
    [168226] = "@VS Contest G/Win Contest 17 as G",
    [168227] = "@VS Contest G/Win Contest 18 as G",
    [168228] = "@VS Contest G/Win Contest 19 as G",
    [168229] = "@VS Contest G/Win Contest 20 as G",
    [168230] = "@VS Contest G/Win Contest 21 as G",
    [168231] = "@VS Contest G/Win Contest 22 as G",
    [168232] = "@VS Contest G/Win Contest 23 as G",
    [168233] = "@VS Contest G/Win Contest 24 as G",
    [168234] = "@VS Contest G/Win Contest 25 as G",
    [168235] = "@VS Contest G/Win Contest 26 as G",
    [168236] = "@VS Contest G/Win Contest 27 as G",
    [168237] = "@VS Contest G/Win Contest 28 as G",
    [168238] = "@VS Contest G/Win Contest 29 as G",
    [168239] = "@VS Contest G/Win Contest 30 as G",
    [168240] = "@VS Contest Bramb/Win Contest 1 as Bramb",
    [168241] = "@VS Contest Bramb/Win Contest 2 as Bramb",
    [168242] = "@VS Contest Bramb/Win Contest 3 as Bramb",
    [168243] = "@VS Contest Bramb/Win Contest 4 as Bramb",
    [168244] = "@VS Contest Bramb/Win Contest 5 as Bramb",
    [168245] = "@VS Contest Bramb/Win Contest 6 as Bramb",
    [168246] = "@VS Contest Bramb/Win Contest 7 as Bramb",
    [168247] = "@VS Contest Bramb/Win Contest 8 as Bramb",
    [168248] = "@VS Contest Bramb/Win Contest 9 as Bramb",
    [168249] = "@VS Contest Bramb/Win Contest 10 as Bramb",
    [168250] = "@VS Contest Bramb/Win Contest 11 as Bramb",
    [168251] = "@VS Contest Bramb/Win Contest 12 as Bramb",
    [168252] = "@VS Contest Bramb/Win Contest 13 as Bramb",
    [168253] = "@VS Contest Bramb/Win Contest 14 as Bramb",
    [168254] = "@VS Contest Bramb/Win Contest 15 as Bramb",
    [168255] = "@VS Contest Bramb/Win Contest 16 as Bramb",
    [168256] = "@VS Contest Bramb/Win Contest 17 as Bramb",
    [168257] = "@VS Contest Bramb/Win Contest 18 as Bramb",
    [168258] = "@VS Contest Bramb/Win Contest 19 as Bramb",
    [168259] = "@VS Contest Bramb/Win Contest 20 as Bramb",
    [168260] = "@VS Contest Bramb/Win Contest 21 as Bramb",
    [168261] = "@VS Contest Bramb/Win Contest 22 as Bramb",
    [168262] = "@VS Contest Bramb/Win Contest 23 as Bramb",
    [168263] = "@VS Contest Bramb/Win Contest 24 as Bramb",
    [168264] = "@VS Contest Bramb/Win Contest 25 as Bramb",
    [168265] = "@VS Contest Bramb/Win Contest 26 as Bramb",
    [168266] = "@VS Contest Bramb/Win Contest 27 as Bramb",
    [168267] = "@VS Contest Bramb/Win Contest 28 as Bramb",
    [168268] = "@VS Contest Bramb/Win Contest 29 as Bramb",
    [168269] = "@VS Contest Bramb/Win Contest 30 as Bramb",
    [168270] = "@VS Contest Develon/Win Contest 1 as Develon",
    [168271] = "@VS Contest Develon/Win Contest 2 as Develon",
    [168272] = "@VS Contest Develon/Win Contest 3 as Develon",
    [168273] = "@VS Contest Develon/Win Contest 4 as Develon",
    [168274] = "@VS Contest Develon/Win Contest 5 as Develon",
    [168275] = "@VS Contest Develon/Win Contest 6 as Develon",
    [168276] = "@VS Contest Develon/Win Contest 7 as Develon",
    [168277] = "@VS Contest Develon/Win Contest 8 as Develon",
    [168278] = "@VS Contest Develon/Win Contest 9 as Develon",
    [168279] = "@VS Contest Develon/Win Contest 10 as Develon",
    [168280] = "@VS Contest Develon/Win Contest 11 as Develon",
    [168281] = "@VS Contest Develon/Win Contest 12 as Develon",
    [168282] = "@VS Contest Develon/Win Contest 13 as Develon",
    [168283] = "@VS Contest Develon/Win Contest 14 as Develon",
    [168284] = "@VS Contest Develon/Win Contest 15 as Develon",
    [168285] = "@VS Contest Develon/Win Contest 16 as Develon",
    [168286] = "@VS Contest Develon/Win Contest 17 as Develon",
    [168287] = "@VS Contest Develon/Win Contest 18 as Develon",
    [168288] = "@VS Contest Develon/Win Contest 19 as Develon",
    [168289] = "@VS Contest Develon/Win Contest 20 as Develon",
    [168290] = "@VS Contest Develon/Win Contest 21 as Develon",
    [168291] = "@VS Contest Develon/Win Contest 22 as Develon",
    [168292] = "@VS Contest Develon/Win Contest 23 as Develon",
    [168293] = "@VS Contest Develon/Win Contest 24 as Develon",
    [168294] = "@VS Contest Develon/Win Contest 25 as Develon",
    [168295] = "@VS Contest Develon/Win Contest 26 as Develon",
    [168296] = "@VS Contest Develon/Win Contest 27 as Develon",
    [168297] = "@VS Contest Develon/Win Contest 28 as Develon",
    [168298] = "@VS Contest Develon/Win Contest 29 as Develon",
    [168299] = "@VS Contest Develon/Win Contest 30 as Develon",
    [168300] = "@VS Contest Gigant/Win Contest 1 as Gigant",
    [168301] = "@VS Contest Gigant/Win Contest 2 as Gigant",
    [168302] = "@VS Contest Gigant/Win Contest 3 as Gigant",
    [168303] = "@VS Contest Gigant/Win Contest 4 as Gigant",
    [168304] = "@VS Contest Gigant/Win Contest 5 as Gigant",
    [168305] = "@VS Contest Gigant/Win Contest 6 as Gigant",
    [168306] = "@VS Contest Gigant/Win Contest 7 as Gigant",
    [168307] = "@VS Contest Gigant/Win Contest 8 as Gigant",
    [168308] = "@VS Contest Gigant/Win Contest 9 as Gigant",
    [168309] = "@VS Contest Gigant/Win Contest 10 as Gigant",
    [168310] = "@VS Contest Gigant/Win Contest 11 as Gigant",
    [168311] = "@VS Contest Gigant/Win Contest 12 as Gigant",
    [168312] = "@VS Contest Gigant/Win Contest 13 as Gigant",
    [168313] = "@VS Contest Gigant/Win Contest 14 as Gigant",
    [168314] = "@VS Contest Gigant/Win Contest 15 as Gigant",
    [168315] = "@VS Contest Gigant/Win Contest 16 as Gigant",
    [168316] = "@VS Contest Gigant/Win Contest 17 as Gigant",
    [168317] = "@VS Contest Gigant/Win Contest 18 as Gigant",
    [168318] = "@VS Contest Gigant/Win Contest 19 as Gigant",
    [168319] = "@VS Contest Gigant/Win Contest 20 as Gigant",
    [168320] = "@VS Contest Gigant/Win Contest 21 as Gigant",
    [168321] = "@VS Contest Gigant/Win Contest 22 as Gigant",
    [168322] = "@VS Contest Gigant/Win Contest 23 as Gigant",
    [168323] = "@VS Contest Gigant/Win Contest 24 as Gigant",
    [168324] = "@VS Contest Gigant/Win Contest 25 as Gigant",
    [168325] = "@VS Contest Gigant/Win Contest 26 as Gigant",
    [168326] = "@VS Contest Gigant/Win Contest 27 as Gigant",
    [168327] = "@VS Contest Gigant/Win Contest 28 as Gigant",
    [168328] = "@VS Contest Gigant/Win Contest 29 as Gigant",
    [168329] = "@VS Contest Gigant/Win Contest 30 as Gigant",
    [168330] = "@VS Contest Madam Luna/Win Contest 1 as Madam Luna",
    [168331] = "@VS Contest Madam Luna/Win Contest 2 as Madam Luna",
    [168332] = "@VS Contest Madam Luna/Win Contest 3 as Madam Luna",
    [168333] = "@VS Contest Madam Luna/Win Contest 4 as Madam Luna",
    [168334] = "@VS Contest Madam Luna/Win Contest 5 as Madam Luna",
    [168335] = "@VS Contest Madam Luna/Win Contest 6 as Madam Luna",
    [168336] = "@VS Contest Madam Luna/Win Contest 7 as Madam Luna",
    [168337] = "@VS Contest Madam Luna/Win Contest 8 as Madam Luna",
    [168338] = "@VS Contest Madam Luna/Win Contest 9 as Madam Luna",
    [168339] = "@VS Contest Madam Luna/Win Contest 10 as Madam Luna",
    [168340] = "@VS Contest Madam Luna/Win Contest 11 as Madam Luna",
    [168341] = "@VS Contest Madam Luna/Win Contest 12 as Madam Luna",
    [168342] = "@VS Contest Madam Luna/Win Contest 13 as Madam Luna",
    [168343] = "@VS Contest Madam Luna/Win Contest 14 as Madam Luna",
    [168344] = "@VS Contest Madam Luna/Win Contest 15 as Madam Luna",
    [168345] = "@VS Contest Madam Luna/Win Contest 16 as Madam Luna",
    [168346] = "@VS Contest Madam Luna/Win Contest 17 as Madam Luna",
    [168347] = "@VS Contest Madam Luna/Win Contest 18 as Madam Luna",
    [168348] = "@VS Contest Madam Luna/Win Contest 19 as Madam Luna",
    [168349] = "@VS Contest Madam Luna/Win Contest 20 as Madam Luna",
    [168350] = "@VS Contest Madam Luna/Win Contest 21 as Madam Luna",
    [168351] = "@VS Contest Madam Luna/Win Contest 22 as Madam Luna",
    [168352] = "@VS Contest Madam Luna/Win Contest 23 as Madam Luna",
    [168353] = "@VS Contest Madam Luna/Win Contest 24 as Madam Luna",
    [168354] = "@VS Contest Madam Luna/Win Contest 25 as Madam Luna",
    [168355] = "@VS Contest Madam Luna/Win Contest 26 as Madam Luna",
    [168356] = "@VS Contest Madam Luna/Win Contest 27 as Madam Luna",
    [168357] = "@VS Contest Madam Luna/Win Contest 28 as Madam Luna",
    [168358] = "@VS Contest Madam Luna/Win Contest 29 as Madam Luna",
    [168359] = "@VS Contest Madam Luna/Win Contest 30 as Madam Luna",
    [168360] = "@VS Contest Dreg/Win Contest 1 as Dreg",
    [168361] = "@VS Contest Dreg/Win Contest 2 as Dreg",
    [168362] = "@VS Contest Dreg/Win Contest 3 as Dreg",
    [168363] = "@VS Contest Dreg/Win Contest 4 as Dreg",
    [168364] = "@VS Contest Dreg/Win Contest 5 as Dreg",
    [168365] = "@VS Contest Dreg/Win Contest 6 as Dreg",
    [168366] = "@VS Contest Dreg/Win Contest 7 as Dreg",
    [168367] = "@VS Contest Dreg/Win Contest 8 as Dreg",
    [168368] = "@VS Contest Dreg/Win Contest 9 as Dreg",
    [168369] = "@VS Contest Dreg/Win Contest 10 as Dreg",
    [168370] = "@VS Contest Dreg/Win Contest 11 as Dreg",
    [168371] = "@VS Contest Dreg/Win Contest 12 as Dreg",
    [168372] = "@VS Contest Dreg/Win Contest 13 as Dreg",
    [168373] = "@VS Contest Dreg/Win Contest 14 as Dreg",
    [168374] = "@VS Contest Dreg/Win Contest 15 as Dreg",
    [168375] = "@VS Contest Dreg/Win Contest 16 as Dreg",
    [168376] = "@VS Contest Dreg/Win Contest 17 as Dreg",
    [168377] = "@VS Contest Dreg/Win Contest 18 as Dreg",
    [168378] = "@VS Contest Dreg/Win Contest 19 as Dreg",
    [168379] = "@VS Contest Dreg/Win Contest 20 as Dreg",
    [168380] = "@VS Contest Dreg/Win Contest 21 as Dreg",
    [168381] = "@VS Contest Dreg/Win Contest 22 as Dreg",
    [168382] = "@VS Contest Dreg/Win Contest 23 as Dreg",
    [168383] = "@VS Contest Dreg/Win Contest 24 as Dreg",
    [168384] = "@VS Contest Dreg/Win Contest 25 as Dreg",
    [168385] = "@VS Contest Dreg/Win Contest 26 as Dreg",
    [168386] = "@VS Contest Dreg/Win Contest 27 as Dreg",
    [168387] = "@VS Contest Dreg/Win Contest 28 as Dreg",
    [168388] = "@VS Contest Dreg/Win Contest 29 as Dreg",
    [168389] = "@VS Contest Dreg/Win Contest 30 as Dreg",
    [168390] = "@VS Contest Woolen/Win Contest 1 as Woolen",
    [168391] = "@VS Contest Woolen/Win Contest 2 as Woolen",
    [168392] = "@VS Contest Woolen/Win Contest 3 as Woolen",
    [168393] = "@VS Contest Woolen/Win Contest 4 as Woolen",
    [168394] = "@VS Contest Woolen/Win Contest 5 as Woolen",
    [168395] = "@VS Contest Woolen/Win Contest 6 as Woolen",
    [168396] = "@VS Contest Woolen/Win Contest 7 as Woolen",
    [168397] = "@VS Contest Woolen/Win Contest 8 as Woolen",
    [168398] = "@VS Contest Woolen/Win Contest 9 as Woolen",
    [168399] = "@VS Contest Woolen/Win Contest 10 as Woolen",
    [168400] = "@VS Contest Woolen/Win Contest 11 as Woolen",
    [168401] = "@VS Contest Woolen/Win Contest 12 as Woolen",
    [168402] = "@VS Contest Woolen/Win Contest 13 as Woolen",
    [168403] = "@VS Contest Woolen/Win Contest 14 as Woolen",
    [168404] = "@VS Contest Woolen/Win Contest 15 as Woolen",
    [168405] = "@VS Contest Woolen/Win Contest 16 as Woolen",
    [168406] = "@VS Contest Woolen/Win Contest 17 as Woolen",
    [168407] = "@VS Contest Woolen/Win Contest 18 as Woolen",
    [168408] = "@VS Contest Woolen/Win Contest 19 as Woolen",
    [168409] = "@VS Contest Woolen/Win Contest 20 as Woolen",
    [168410] = "@VS Contest Woolen/Win Contest 21 as Woolen",
    [168411] = "@VS Contest Woolen/Win Contest 22 as Woolen",
    [168412] = "@VS Contest Woolen/Win Contest 23 as Woolen",
    [168413] = "@VS Contest Woolen/Win Contest 24 as Woolen",
    [168414] = "@VS Contest Woolen/Win Contest 25 as Woolen",
    [168415] = "@VS Contest Woolen/Win Contest 26 as Woolen",
    [168416] = "@VS Contest Woolen/Win Contest 27 as Woolen",
    [168417] = "@VS Contest Woolen/Win Contest 28 as Woolen",
    [168418] = "@VS Contest Woolen/Win Contest 29 as Woolen",
    [168419] = "@VS Contest Woolen/Win Contest 30 as Woolen",
    [168420] = "@VS Contest Packy/Win Contest 1 as Packy",
    [168421] = "@VS Contest Packy/Win Contest 2 as Packy",
    [168422] = "@VS Contest Packy/Win Contest 3 as Packy",
    [168423] = "@VS Contest Packy/Win Contest 4 as Packy",
    [168424] = "@VS Contest Packy/Win Contest 5 as Packy",
    [168425] = "@VS Contest Packy/Win Contest 6 as Packy",
    [168426] = "@VS Contest Packy/Win Contest 7 as Packy",
    [168427] = "@VS Contest Packy/Win Contest 8 as Packy",
    [168428] = "@VS Contest Packy/Win Contest 9 as Packy",
    [168429] = "@VS Contest Packy/Win Contest 10 as Packy",
    [168430] = "@VS Contest Packy/Win Contest 11 as Packy",
    [168431] = "@VS Contest Packy/Win Contest 12 as Packy",
    [168432] = "@VS Contest Packy/Win Contest 13 as Packy",
    [168433] = "@VS Contest Packy/Win Contest 14 as Packy",
    [168434] = "@VS Contest Packy/Win Contest 15 as Packy",
    [168435] = "@VS Contest Packy/Win Contest 16 as Packy",
    [168436] = "@VS Contest Packy/Win Contest 17 as Packy",
    [168437] = "@VS Contest Packy/Win Contest 18 as Packy",
    [168438] = "@VS Contest Packy/Win Contest 19 as Packy",
    [168439] = "@VS Contest Packy/Win Contest 20 as Packy",
    [168440] = "@VS Contest Packy/Win Contest 21 as Packy",
    [168441] = "@VS Contest Packy/Win Contest 22 as Packy",
    [168442] = "@VS Contest Packy/Win Contest 23 as Packy",
    [168443] = "@VS Contest Packy/Win Contest 24 as Packy",
    [168444] = "@VS Contest Packy/Win Contest 25 as Packy",
    [168445] = "@VS Contest Packy/Win Contest 26 as Packy",
    [168446] = "@VS Contest Packy/Win Contest 27 as Packy",
    [168447] = "@VS Contest Packy/Win Contest 28 as Packy",
    [168448] = "@VS Contest Packy/Win Contest 29 as Packy",
    [168449] = "@VS Contest Packy/Win Contest 30 as Packy",
    [168450] = "@VS Contest Monsta/Win Contest 1 as Monsta",
    [168451] = "@VS Contest Monsta/Win Contest 2 as Monsta",
    [168452] = "@VS Contest Monsta/Win Contest 3 as Monsta",
    [168453] = "@VS Contest Monsta/Win Contest 4 as Monsta",
    [168454] = "@VS Contest Monsta/Win Contest 5 as Monsta",
    [168455] = "@VS Contest Monsta/Win Contest 6 as Monsta",
    [168456] = "@VS Contest Monsta/Win Contest 7 as Monsta",
    [168457] = "@VS Contest Monsta/Win Contest 8 as Monsta",
    [168458] = "@VS Contest Monsta/Win Contest 9 as Monsta",
    [168459] = "@VS Contest Monsta/Win Contest 10 as Monsta",
    [168460] = "@VS Contest Monsta/Win Contest 11 as Monsta",
    [168461] = "@VS Contest Monsta/Win Contest 12 as Monsta",
    [168462] = "@VS Contest Monsta/Win Contest 13 as Monsta",
    [168463] = "@VS Contest Monsta/Win Contest 14 as Monsta",
    [168464] = "@VS Contest Monsta/Win Contest 15 as Monsta",
    [168465] = "@VS Contest Monsta/Win Contest 16 as Monsta",
    [168466] = "@VS Contest Monsta/Win Contest 17 as Monsta",
    [168467] = "@VS Contest Monsta/Win Contest 18 as Monsta",
    [168468] = "@VS Contest Monsta/Win Contest 19 as Monsta",
    [168469] = "@VS Contest Monsta/Win Contest 20 as Monsta",
    [168470] = "@VS Contest Monsta/Win Contest 21 as Monsta",
    [168471] = "@VS Contest Monsta/Win Contest 22 as Monsta",
    [168472] = "@VS Contest Monsta/Win Contest 23 as Monsta",
    [168473] = "@VS Contest Monsta/Win Contest 24 as Monsta",
    [168474] = "@VS Contest Monsta/Win Contest 25 as Monsta",
    [168475] = "@VS Contest Monsta/Win Contest 26 as Monsta",
    [168476] = "@VS Contest Monsta/Win Contest 27 as Monsta",
    [168477] = "@VS Contest Monsta/Win Contest 28 as Monsta",
    [168478] = "@VS Contest Monsta/Win Contest 29 as Monsta",
    [168479] = "@VS Contest Monsta/Win Contest 30 as Monsta",
    [168480] = "@VS Contest Maita/Win Contest 1 as Maita",
    [168481] = "@VS Contest Maita/Win Contest 2 as Maita",
    [168482] = "@VS Contest Maita/Win Contest 3 as Maita",
    [168483] = "@VS Contest Maita/Win Contest 4 as Maita",
    [168484] = "@VS Contest Maita/Win Contest 5 as Maita",
    [168485] = "@VS Contest Maita/Win Contest 6 as Maita",
    [168486] = "@VS Contest Maita/Win Contest 7 as Maita",
    [168487] = "@VS Contest Maita/Win Contest 8 as Maita",
    [168488] = "@VS Contest Maita/Win Contest 9 as Maita",
    [168489] = "@VS Contest Maita/Win Contest 10 as Maita",
    [168490] = "@VS Contest Maita/Win Contest 11 as Maita",
    [168491] = "@VS Contest Maita/Win Contest 12 as Maita",
    [168492] = "@VS Contest Maita/Win Contest 13 as Maita",
    [168493] = "@VS Contest Maita/Win Contest 14 as Maita",
    [168494] = "@VS Contest Maita/Win Contest 15 as Maita",
    [168495] = "@VS Contest Maita/Win Contest 16 as Maita",
    [168496] = "@VS Contest Maita/Win Contest 17 as Maita",
    [168497] = "@VS Contest Maita/Win Contest 18 as Maita",
    [168498] = "@VS Contest Maita/Win Contest 19 as Maita",
    [168499] = "@VS Contest Maita/Win Contest 20 as Maita",
    [168500] = "@VS Contest Maita/Win Contest 21 as Maita",
    [168501] = "@VS Contest Maita/Win Contest 22 as Maita",
    [168502] = "@VS Contest Maita/Win Contest 23 as Maita",
    [168503] = "@VS Contest Maita/Win Contest 24 as Maita",
    [168504] = "@VS Contest Maita/Win Contest 25 as Maita",
    [168505] = "@VS Contest Maita/Win Contest 26 as Maita",
    [168506] = "@VS Contest Maita/Win Contest 27 as Maita",
    [168507] = "@VS Contest Maita/Win Contest 28 as Maita",
    [168508] = "@VS Contest Maita/Win Contest 29 as Maita",
    [168509] = "@VS Contest Maita/Win Contest 30 as Maita",
}

local function set_location_checked(ap_id)
    local code = location_map[ap_id] or tarot_location_map[ap_id] or vs_story_simple_location_map[ap_id] or vs_contest_simple_location_map[ap_id] or vs_story_complete_location_map[ap_id] or vs_contest_complete_location_map[ap_id]
    if not code then
        print("PB4 PopTracker: unmapped checked location ID " .. tostring(ap_id))
        return
    end

    local obj = Tracker:FindObjectForCode(code)
    if not obj then
        print("PB4 PopTracker: mapped location object not found: " .. tostring(code))
        return
    end

    -- Match A Hat in Time's AP location handler: one confirmed check
    -- consumes one available chest instead of forcing the counter to zero.
    if obj.AvailableChestCount > 0 then
        obj.AvailableChestCount = obj.AvailableChestCount - 1
    end
    print("PB4 PopTracker: check received: " .. tostring(ap_id) .. " -> " .. tostring(code))
end



local vs_story_character_names = {
    "Bub", "Alkanet", "Marino", "Kurol", "Tamtam", "Cleon", "G", "Bramb",
    "Develon", "Gigant", "Madam Luna", "Dreg", "Woolen", "Packy", "Monsta", "Maita"
}
local vs_story_enemy_names = {
    "Bob", "Alkanet", "Marino", "Kurol", "Tamtam", "Cleon", "G", "Bramb",
    "Develon", "Gigant", "Secret", "Madam Luna", "Dreg"
}
local vs_story_selected = {}
local vs_story_start_character = ""
local vs_story_enemy_order = vs_story_enemy_names
local vs_story_extra_enemy = false

local function character_code(character)
    return "vs_story_character_" .. string.lower(string.gsub(character, "[^%w]+", "_"))
end

-- VS/Contest Simple square stays red until the Unlock VS/Contest
-- item is received. The square then becomes available/green.
function vs_contest_simple_unlocked()
    local unlock = Tracker:FindObjectForCode("Unlock VS/Contest")
    return (unlock and unlock.Active) and 1 or 0
end

-- VS/Story Simple map: all opponent squares stay locked until the
-- Unlock VS/Story item is received. Checked squares then use their normal
-- checked-state artwork, so no extra progression checks are needed.
function vs_story_simple_unlocked()
    local unlock = Tracker:FindObjectForCode("Unlock VS/Story")
    return (unlock and unlock.Active) and 1 or 0
end

-- VS/Story Simple's Secret check is available only when Extra Enemy is enabled.
function vs_story_simple_secret_access()
    local option = Tracker:FindObjectForCode("extra_enemy")
    return (option and option.Active) and 1 or 0
end

-- Custom PopTracker access rule: a row requires its character unlock;
-- each later opponent requires one more Progressive Enemy.
function vs_story_access(character, enemy_index)
    local char_item = Tracker:FindObjectForCode(character_code(character))
    if character ~= vs_story_start_character and (not char_item or not char_item.Active) then
        return 0
    end
    if not vs_story_selected[character] then return 0 end

    local enemy_count = 0
    local enemy_item = Tracker:FindObjectForCode("progressive_enemy")
    if enemy_item then enemy_count = enemy_item.AcquiredCount or 0 end

    local index = tonumber(enemy_index) or 0
    if index == 10 and not vs_story_extra_enemy then return 0 end
    -- Skip Secret in progression order when the option is disabled.
    local required = index
    if not vs_story_extra_enemy and index > 10 then required = index - 1 end
    return enemy_count >= required and 1 or 0
end

local function initialize_vs_story_slot(slot_data)
    local extra_enemy_item = Tracker:FindObjectForCode("extra_enemy")
    if extra_enemy_item then
        extra_enemy_item.Active = (slot_data and (
            slot_data["extra_enemy"] == 1
            or (slot_data["vs_story_options"] and slot_data["vs_story_options"]["extra_enemy"] == 1)
        )) or false
    end
    vs_story_selected = {}
    vs_story_start_character = tostring((slot_data and slot_data["vs_story_start_character"]) or "")
    if vs_story_start_character == "Bob" then vs_story_start_character = "Bub" end
    vs_story_extra_enemy = (slot_data and (
        slot_data["extra_enemy"] == 1
        or (slot_data["vs_story_options"] and slot_data["vs_story_options"]["extra_enemy"] == 1)
    )) or false

    for _, character in ipairs((slot_data and slot_data["vs_story_selected_characters"]) or {}) do
        character = tostring(character)
        if character == "Bob" then character = "Bub" end
        vs_story_selected[character] = true
    end

    -- Clear every character unlock first; then enable only the starting character.
    for _, character in ipairs(vs_story_character_names) do
        local item = Tracker:FindObjectForCode(character_code(character))
        if item then item.Active = (character == vs_story_start_character and vs_story_selected[character]) end
    end
    local progressive = Tracker:FindObjectForCode("progressive_enemy")
    if progressive then progressive.AcquiredCount = 0 end
    print("PB4 PopTracker: new AP session initialized; start=" .. vs_story_start_character
        .. "; selected=" .. tostring(#((slot_data and slot_data["vs_story_selected_characters"]) or {})))
end

-- VS/Contest Complete progression state comes from the connected AP slot.
local vs_contest_character_names = {
    "Bub", "Alkanet", "Marino", "Kurol", "Tamtam", "Cleon", "G", "Bramb",
    "Develon", "Gigant", "Madam Luna", "Dreg", "Woolen", "Packy", "Monsta", "Maita"
}
local vs_contest_selected = {}
local vs_contest_start_character = ""
local vs_contest_win_count = 0

local function vs_contest_character_code(character)
    if character == "Bub" then return "vs_contest_character_bob" end
    return "vs_contest_character_" .. string.lower(string.gsub(character, "[^%w]+", "_"))
end

function vs_contest_character_access(character)
    character = tostring(character or "")
    if not vs_contest_selected[character] then return 0 end
    if character == vs_contest_start_character then return 1 end
    local unlock = Tracker:FindObjectForCode(vs_contest_character_code(character))
    return (unlock and unlock.Active) and 1 or 0
end

function vs_contest_win_visible(number)
    local n = tonumber(number) or 0
    return n >= 1 and n <= vs_contest_win_count
end

local function initialize_vs_contest_slot(slot_data)
    vs_contest_selected = {}
    vs_contest_start_character = tostring((slot_data and slot_data["contest_start_character"]) or "")
    vs_contest_win_count = tonumber((slot_data and slot_data["custom_contest_wins"]) or
        (slot_data and slot_data["vs_contest_options"] and slot_data["vs_contest_options"]["Custom_contest_wins"])) or 0
    for _, character in ipairs((slot_data and slot_data["contest_selected_characters"]) or {}) do
        vs_contest_selected[tostring(character)] = true
    end
    for _, character in ipairs(vs_contest_character_names) do
        local item = Tracker:FindObjectForCode(vs_contest_character_code(character))
        if item then item.Active = (character == vs_contest_start_character and vs_contest_selected[character]) or false end
    end
    print("PB4 PopTracker: VS/Contest initialized; start=" .. vs_contest_start_character ..
        "; selected=" .. tostring(#((slot_data and slot_data["contest_selected_characters"]) or {})) ..
        "; wins=" .. tostring(vs_contest_win_count))
end


-- Seed-based visibility for Puzzle/Arcade and Puzzle/Story.
-- These internal toggles are not included in layouts; they only drive visibility_rules.
local function set_visibility_flag(code, active)
    local item = Tracker:FindObjectForCode(code)
    if item then item.Active = active and true or false end
end

local arcade_letters_all = {
    "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M",
    "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "?", "X", "Y", "Z"
}
local story_cards_all = {
    "Fool", "Star", "Moon", "Hermit", "Tower", "Chariot", "Death", "Devil",
    "Hanged Man", "Magician", "Wheel of Fortune", "Judgement", "Lovers",
    "Temperance", "Hierophant", "Emperor", "Empress", "High Priestess",
    "Sun", "Justice", "Strength", "World"
}

local function initialize_mode_visibility(slot_data)
    slot_data = slot_data or {}
    local modes = slot_data["game_modes"] or {}
    local arcade_complete = tonumber(modes["puzzle_arcade_complete"] or slot_data["puzzle_arcade_complete"] or slot_data["puzzle_arcade"] or 0) == 1
    local arcade_simple = tonumber(modes["puzzle_arcade_simple"] or slot_data["puzzle_arcade_simple"] or 0) == 1
    local story_complete = tonumber(modes["puzzle_story_complete"] or slot_data["puzzle_story_complete"] or 0) == 1
    local story_simple = tonumber(modes["puzzle_story_simple"] or slot_data["puzzle_story_simple"] or 0) == 1
    local vs_story_simple = tonumber(modes["vs_story_simple"] or slot_data["vs_story_simple"] or 0) == 1
    local vs_story_complete = tonumber(modes["vs_story_complete"] or slot_data["vs_story_complete"] or 0) == 1
    local vs_contest_simple = tonumber(modes["vs_contest_simple"] or slot_data["vs_contest_simple"] or 0) == 1
    local vs_contest_complete = tonumber(modes["vs_contest_complete"] or slot_data["vs_contest_complete"] or 0) == 1

    local extra_enemy_enabled = tonumber(slot_data["extra_enemy"] or
        (slot_data["vs_story_options"] and slot_data["vs_story_options"]["extra_enemy"]) or 0) == 1
    set_visibility_flag("pb4_vs_story_simple_secret", vs_story_simple and extra_enemy_enabled)
    set_visibility_flag("pb4_vs_story_complete_secret", vs_story_complete and extra_enemy_enabled)

    set_visibility_flag("pb4_vs_story_simple", vs_story_simple)
    set_visibility_flag("pb4_vs_story_complete", vs_story_complete)
    set_visibility_flag("pb4_vs_contest_simple", vs_contest_simple)
    set_visibility_flag("pb4_vs_contest_complete", vs_contest_complete)

    -- Custom Contest Wins is sent flat as custom_contest_wins.
    -- Keep the nested slot_data field as a compatibility fallback.
    local contest_wins = tonumber(slot_data["custom_contest_wins"] or
        (slot_data["vs_contest_options"] and slot_data["vs_contest_options"]["Custom_contest_wins"])) or 0
    for number = 1, 30 do
        set_visibility_flag("pb4_contest_wins_" .. number, number <= contest_wins)
    end

    local active_letters = {}
    if arcade_complete then
        for _, letter in ipairs(arcade_letters_all) do active_letters[letter] = true end
    elseif arcade_simple then
        active_letters["A"] = true
        active_letters["B"] = true
        local selected = modes["puzzle_arcade_simple_letters"] or slot_data["puzzle_arcade_simple_letters"] or {}
        for _, letter in ipairs(selected) do active_letters[tostring(letter)] = true end
    end
    for _, letter in ipairs(arcade_letters_all) do
        set_visibility_flag("pb4_arcade_" .. letter, active_letters[letter] == true)
    end

    local active_cards = {}
    if story_complete then
        for _, card in ipairs(story_cards_all) do active_cards[card] = true end
    elseif story_simple then
        active_cards["Fool"] = true
        local selected = modes["puzzle_story_simple_cards"] or slot_data["puzzle_story_simple_cards"] or {}
        for _, card in ipairs(selected) do active_cards[tostring(card)] = true end
    end
    for _, card in ipairs(story_cards_all) do
        local code = "pb4_story_" .. string.lower(string.gsub(card, "%s+", "_"))
        set_visibility_flag(code, active_cards[card] == true)
    end

    print("PB4 PopTracker: map visibility initialized; Arcade=" ..
        (arcade_complete and "Complete" or (arcade_simple and "Simple" or "Disabled")) ..
        "; Story=" .. (story_complete and "Complete" or (story_simple and "Simple" or "Disabled")) ..
        "; VS/Story=" .. (vs_story_complete and "Complete" or (vs_story_simple and "Simple" or "Disabled")) ..
        "; VS/Contest=" .. (vs_contest_complete and "Complete" or (vs_contest_simple and "Simple" or "Disabled")) ..
        "; Contest wins=" .. tostring(contest_wins))
end


Tracker:AddLocations("locations/worlds.json")

Archipelago:AddClearHandler("pb4_clear", function(slot_data)
    print("PB4 PopTracker: Archipelago slot connected; initializing location counters.")
    for _, code in pairs(location_map) do
        local section = Tracker:FindObjectForCode(code)
        if section then section.AvailableChestCount = section.ChestCount end
    end
    for _, code in pairs(tarot_location_map) do
        local section = Tracker:FindObjectForCode(code)
        if section then section.AvailableChestCount = section.ChestCount end
    end
    for _, code in pairs(vs_story_complete_location_map) do
        local section = Tracker:FindObjectForCode(code)
        if section then section.AvailableChestCount = section.ChestCount end
    end
    for _, code in pairs(vs_contest_complete_location_map) do
        local section = Tracker:FindObjectForCode(code)
        if section then section.AvailableChestCount = section.ChestCount end
    end
    for _, code in pairs(item_map) do
        local item = Tracker:FindObjectForCode(code)
        if item then item.Active = false end
    end
    initialize_mode_visibility(slot_data)
    initialize_vs_story_slot(slot_data)
    initialize_vs_contest_slot(slot_data)

    -- Archipelago's CheckedLocations is the authoritative snapshot for this
    -- connection. Reapply it after clearing, so a new seed cannot inherit old checks.
    if Archipelago.CheckedLocations then
        for _, ap_id in ipairs(Archipelago.CheckedLocations) do
            set_location_checked(ap_id)
        end
        print("PB4 PopTracker: restored " .. tostring(#Archipelago.CheckedLocations) .. " checked locations from this session.")
    else
        print("PB4 PopTracker: CheckedLocations unavailable; live checks will still be tracked.")
    end
end)

Archipelago:AddLocationHandler("pb4_location_checked", function(ap_id, location_name)
    set_location_checked(ap_id)
end)

Archipelago:AddItemHandler("pb4_item_received", function(index, ap_id, item_name, player_number)
    local code = item_map[ap_id]
    if code then
        local item = Tracker:FindObjectForCode(code)
        if item then item.Active = true end
    end

    if ap_id >= 200200 and ap_id <= 200215 then
        local character = vs_story_character_names[ap_id - 200200 + 1]
        local char_item = character and Tracker:FindObjectForCode(character_code(character))
        if char_item and vs_story_selected[character] then
            char_item.Active = true
            print("PB4 PopTracker: character unlock received: " .. character)
        end
    elseif ap_id >= 200230 and ap_id <= 200245 then
        local character = vs_contest_character_names[ap_id - 200230 + 1]
        local contest_item = character and Tracker:FindObjectForCode(vs_contest_character_code(character))
        if contest_item and vs_contest_selected[character] then
            contest_item.Active = true
            print("PB4 PopTracker: VS/Contest character unlock received: " .. character)
        end
    elseif ap_id == 200220 then
        local progressive = Tracker:FindObjectForCode("progressive_enemy")
        if progressive then
            progressive.AcquiredCount = math.min(progressive.AcquiredCount + 1, progressive.MaxCount)
        end
        print("PB4 PopTracker: Progressive Enemy received.")
    end
end)
