import bpy

# 1. Define your custom operator (what you want to run)
class MONOGAME_OT_run_script(bpy.types.Operator):
    bl_idname = "monogame.run_script"
    bl_label = "Run Platformer Export"

    def execute(self, context):
        text = bpy.data.texts.get('ExportScript') or bpy.data.texts.get('ExportScript.py')
        if text is not None:
            try:
                exec(text.as_string(), {})
                self.report({'INFO'}, "ExportScript executed successfully.")
            except Exception as e:
                self.report({'ERROR'}, f"Error running ExportScript: {e}")
                return {'CANCELLED'}
            return {'FINISHED'}
        else:
            self.report({'ERROR'}, "ExportScript not found in Text Editor.")
            return {'CANCELLED'}

# 2. Define a custom menu
class MONOGAME_MT_root_menu(bpy.types.Menu):
    bl_label = "Platformer"
    bl_idname = "MONOGAME_MT_root_menu"

    def draw(self, context):
        layout = self.layout
        layout.operator("monogame.run_script", icon='PLAY')

# 3. Add the new menu to the 3D View's header
def draw_menu(self, context):
    self.layout.menu(MONOGAME_MT_root_menu.bl_idname)

def register():
    bpy.utils.register_class(MONOGAME_OT_run_script)
    bpy.utils.register_class(MONOGAME_MT_root_menu)
    bpy.types.VIEW3D_MT_editor_menus.append(draw_menu)  # Adds to the top-level header

def unregister():
    bpy.types.VIEW3D_MT_editor_menus.remove(draw_menu)
    bpy.utils.unregister_class(MONOGAME_MT_root_menu)
    bpy.utils.unregister_class(MONOGAME_OT_run_script)

if __name__ == "__main__":
    register()