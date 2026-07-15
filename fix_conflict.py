import os
import re

filepath = r"Assets\_Project\Scenes\Gameplay\VietnamStreet.unity"
with open(filepath, 'r', encoding='utf-8') as f:
    lines = f.readlines()

out_lines = []
i = 0
while i < len(lines):
    line = lines[i]
    if line.startswith("<<<<<<< HEAD"):
        # We might have the PrefabInstance block or the children array block
        # Let's check what conflict this is.
        # If it's line 704 (the PrefabInstance start)
        if "--- !u!1001" in lines[i+1]:
            # This is the 3-part conflict for PrefabInstance.
            # Let's parse the whole PrefabInstance block.
            # Block ends at the final >>>>>>> 3add0... (which is at line 827)
            # But wait, there are 3 conflict markers in this single object!
            # It's easier to just read until we hit the next GameObject or PrefabInstance (--- !u!...)
            
            # Since we know EXACTLY the structure of this conflict from lines 704 to 827, let's extract it.
            # Wait, the end of the PrefabInstance block is line 827.
            # Next block starts at line 828 with `--- !u!1 &238444957`
            
            j = i
            while j < len(lines) and not lines[j].startswith("--- !u!1 &238444957"):
                j += 1
            
            conflict_block = lines[i:j]
            
            # Now we have the whole conflict_block. Let's parse out HEAD and THEIRS.
            # A state machine to extract HEAD lines and THEIRS lines.
            # But notice that there are common lines!
            # e.g., `PrefabInstance:` is common, not inside markers.
            
            head_lines = []
            theirs_lines = []
            
            state = "COMMON"
            for cl in conflict_block:
                if cl.startswith("<<<<<<< HEAD"):
                    state = "HEAD"
                elif cl.startswith("======="):
                    state = "THEIRS"
                elif cl.startswith(">>>>>>>"):
                    state = "COMMON"
                else:
                    if state == "COMMON":
                        head_lines.append(cl)
                        theirs_lines.append(cl)
                    elif state == "HEAD":
                        head_lines.append(cl)
                    elif state == "THEIRS":
                        theirs_lines.append(cl)
            
            out_lines.extend(head_lines)
            out_lines.extend(theirs_lines)
            
            i = j
            continue

        elif "- {fileID:" in lines[i+1]:
            # This is the children array conflict at the end of the file
            # lines 8386 to 8396
            j = i
            while j < len(lines) and not lines[j].startswith(">>>>>>>"):
                j += 1
            
            conflict_block = lines[i:j+1]
            
            for cl in conflict_block:
                if not (cl.startswith("<<<<<<<") or cl.startswith("=======") or cl.startswith(">>>>>>>")):
                    out_lines.append(cl)
            
            i = j + 1
            continue

    out_lines.append(line)
    i += 1

with open(filepath, 'w', encoding='utf-8') as f:
    f.writelines(out_lines)
print("Conflict resolved successfully!")
