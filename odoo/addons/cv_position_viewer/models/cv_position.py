from odoo import fields, models


class CvPosition(models.Model):
    _name = 'cv.position'
    _description = 'CVPlatform Position'
    _order = 'external_id desc'
    _rec_name = 'title'

    external_id = fields.Integer(string='External ID', required=True, index=True)
    title = fields.Char(string='Title', required=True)
    company = fields.Char(string='Company')
    level = fields.Char(string='Level')
    short_description = fields.Text(string='Short Description')
    published_cv_count = fields.Integer(string='Published CV Count', default=0)
    last_synced_at = fields.Datetime(string='Last Synced At')
    api_base_url = fields.Char(string='API Base URL')
    attribute_ids = fields.One2many('cv.position.attribute', 'position_id', string='Attributes')

    _sql_constraints = [
        ('external_id_uniq', 'unique(external_id)', 'A position with this external ID already exists.'),
    ]

    def action_sync(self):
        self.ensure_one()
        return {
            'type': 'ir.actions.act_window',
            'name': 'Sync Position',
            'res_model': 'cv.position.import.wizard',
            'view_mode': 'form',
            'target': 'new',
            'context': {
                'default_position_id': self.id,
                'default_api_base_url': self.api_base_url,
            },
        }


class CvPositionAttribute(models.Model):
    _name = 'cv.position.attribute'
    _description = 'CVPlatform Position Attribute'
    _order = 'attribute_id asc'
    _rec_name = 'name'

    position_id = fields.Many2one('cv.position', string='Position', required=True, ondelete='cascade')
    attribute_id = fields.Integer(string='Attribute ID', required=True)
    name = fields.Char(string='Attribute Name', required=True)
    attribute_type = fields.Char(string='Type', required=True)
    is_required = fields.Boolean(string='Required', default=False)
    response_count = fields.Integer(string='Responses', default=0)
    summary = fields.Text(string='Summary')
    numeric_min = fields.Float(string='Numeric Min')
    numeric_max = fields.Float(string='Numeric Max')
    numeric_average = fields.Float(string='Numeric Average')
    date_min = fields.Char(string='Earliest Date')
    date_max = fields.Char(string='Latest Date')
    date_range_earliest_start = fields.Char(string='Earliest Start')
    date_range_latest_end = fields.Char(string='Latest End')
    boolean_true_count = fields.Integer(string='True Count', default=0)
    boolean_false_count = fields.Integer(string='False Count', default=0)
    markdown_avg_length = fields.Float(string='Avg Markdown Length')
    value_breakdown_ids = fields.One2many('cv.position.attribute.value', 'attribute_line_id', string='Breakdown')


class CvPositionAttributeValue(models.Model):
    _name = 'cv.position.attribute.value'
    _description = 'Attribute Option Breakdown'
    _order = 'count desc, value asc'
    _rec_name = 'value'

    attribute_line_id = fields.Many2one('cv.position.attribute', string='Attribute', required=True, ondelete='cascade')
    value = fields.Char(string='Value / Option', required=True)
    count = fields.Integer(string='Count', default=0)
