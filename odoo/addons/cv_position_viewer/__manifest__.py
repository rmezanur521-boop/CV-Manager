{
    'name': 'CV Position Viewer',
    'version': '17.0.1.0.0',
    'category': 'Human Resources',
    'summary': 'Read-only viewer and importer for CVPlatform position aggregates',
    'author': 'CVPlatform Team',
    'depends': ['base', 'web'],
    'data': [
        'security/ir.model.access.csv',
        'data/ir_config_parameter.xml',
        'wizard/cv_position_import_wizard_views.xml',
        'views/cv_position_views.xml',
        'views/cv_position_menus.xml',
    ],
    'installable': True,
    'application': True,
    'auto_install': False,
    'license': 'LGPL-3',
}
