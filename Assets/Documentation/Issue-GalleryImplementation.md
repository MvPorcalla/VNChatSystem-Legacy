# 🎨 **CG Gallery System Architecture**

You need a **persistent gallery system** separate from dialogue saves. Here's the complete solution:

---

## **📁 Step 1: Update PlayerData.cs**

Add CG tracking to your player profile (survives chat resets):---

## **💾 Step 2: Update PlayerSaveManager.cs**

Add CG unlock methods:---

## **📨 Step 3: Update MessageData.cs**

Add CG unlock metadata to messages:---

## **🎯 Step 4: Update MugiParser.cs**

Add CG unlock syntax to MUGI files:---

## **💬 Step 5: Update ChatDisplayManager.cs**

Unlock CGs when displaying images:---

## **🖼️ Step 6: Create GalleryManager.cs**

For displaying CGs in HomeScreen:---

## **🎴 Step 7: Create GalleryItem.cs**

Individual gallery thumbnail:---

## **📝 Step 8: MUGI File Syntax**

Update your MUGI files to mark CG unlocks:

```mugi
# Example MUGI file with CG unlock

==emma_beach_scene==
emma: "Want to see something cool?"
emma: "Check this out!"

# Normal image (doesn't unlock in gallery)
img: Assets/CGs/emma/casual.png

# CG unlock syntax (unlocks in gallery)
img: Assets/CGs/emma/beach.png [cg:emma_beach_01]

emma: "Isn't it beautiful?"
```

**Syntax explanation:**
- `img: path/to/image.png` → Normal image
- `img: path/to/image.png [cg:unique_id]` → Unlocks in gallery with ID

---

## **✅ Complete Workflow**

### **When CG Unlocks:**
```
1. Player reaches CG image in chat
2. ChatDisplayManager displays image
3. Detects unlocksCG = true
4. Calls PlayerSaveManager.UnlockCG()
5. CG saved to player_data.json
6. (Optional) Shows unlock notification
```

### **When Opening Gallery:**
```
1. Player opens HomeScreen
2. Clicks Gallery button
3. GalleryManager.RefreshGallery() loads from PlayerSaveManager
4. Displays all unlocked CGs
5. Clicking thumbnail opens fullscreen viewer
```

### **When Chat Resets:**
```
1. DialogueSaveManager.ClearChatState() deletes chat history
2. PlayerSaveManager data is UNTOUCHED
3. CGs remain in gallery ✅
```

---

## **🎯 Key Features**

✅ **Persistent across resets** - CGs saved in PlayerData, not ChatState  
✅ **"NEW" badges** - Shows which CGs haven't been viewed in gallery  
✅ **Character sorting** - Filter by character name  
✅ **Fullscreen viewer** - Click thumbnail to view full image  
✅ **Stats tracking** - Shows X/Y unlocked  
✅ **Addressables** - Async loading with proper cleanup  

---

## **🔧 Setup Checklist**

1. ✅ Add `UnlockedCG` class to PlayerData.cs
2. ✅ Add CG methods to PlayerSaveManager.cs
3. ✅ Add CG fields to MessageData.cs
4. ✅ Update MugiParser to parse `[cg:id]` syntax
5. ✅ Update ChatDisplayManager to call UnlockCG()
6. ✅ Create GalleryManager in HomeScreen
7. ✅ Create GalleryItem prefab with thumbnail
8. ✅ Update MUGI files with CG tags
9. ✅ Test unlock → reset → gallery persistence

Your CGs will now persist forever, even if players reset the story! 🎨