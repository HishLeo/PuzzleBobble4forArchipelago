-- Puzzle Bobble 4 PopTracker AP auto-tracking.
-- Uses actual IDs from the Puzzle Bobble 4 .apworld.
Tracker:AddMaps("maps/maps.json")
Tracker:AddLocations("locations/worlds.json")
Tracker:AddItems("items/items.json")
Tracker:AddLayouts("layouts/default.json")

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
}

local function set_location_checked(ap_id)
    local code = location_map[ap_id]
    if code then
        local section = Tracker:FindObjectForCode(code)
        if section then section.AvailableChestCount = 0 end
    end
end

Archipelago:AddClearHandler("pb4_clear", function(slot_data)
    for _, code in pairs(location_map) do
        local section = Tracker:FindObjectForCode(code)
        if section then section.AvailableChestCount = section.ChestCount end
    end
    for _, code in pairs(item_map) do
        local item = Tracker:FindObjectForCode(code)
        if item then item.Active = false end
    end
    if Archipelago.CheckedLocations then
        for _, ap_id in ipairs(Archipelago.CheckedLocations) do
            set_location_checked(ap_id)
        end
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
end)
